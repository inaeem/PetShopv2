using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PetShop.Data.Context;
using Xunit;

namespace PetShop.Tests.E2E;

/// <summary>
/// Boots the real API in-process against a throwaway database on a real SQL Server,
/// so end-to-end tests exercise the full pipeline — auth, filters, validation,
/// service and data layers, and the stored procedure — the way a client would.
///
/// No container/Docker: the tests use whatever SQL Server you point them at.
///   - Default: SQL Server LocalDB (Server=(localdb)\MSSQLLocalDB), typical on Windows dev boxes.
///   - Override: set the PETSHOP_TEST_CONNECTION environment variable to any reachable
///     SQL Server (e.g. a shared QA/test instance, or a developer's local install).
///
/// A uniquely-named database is created by the app's startup migration and dropped
/// on disposal, so runs are isolated and self-cleaning.
/// </summary>
public class PetShopApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Connection to the SQL Server (pointing at master); the test DB name is swapped in below.
    private static string BaseConnection =>
        Environment.GetEnvironmentVariable("PETSHOP_TEST_CONNECTION")
        ?? @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly string _databaseName = $"PetShop_E2E_{Guid.NewGuid():N}";

    private string TestConnectionString =>
        new SqlConnectionStringBuilder(BaseConnection) { InitialCatalog = _databaseName }.ConnectionString;

    // The API verifies incoming JWTs with the issuer's RSA public key (it no longer
    // issues them). Tests own a throwaway RSA keypair: they sign tokens with the private
    // key via CreateToken, and hand the API the public modulus/exponent through config.
    private static readonly RSA JwtRsa = RSA.Create(2048);
    private const string JwtIssuer = "PetShop.Api";
    private const string JwtAudience = "PetShop.Clients";

    /// <summary>Creates a bearer token signed with the test RSA private key, carrying the given roles.</summary>
    public string CreateToken(params string[] roles)
    {
        var creds = new SigningCredentials(
            new RsaSecurityKey(JwtRsa), SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim> { new(ClaimTypes.Name, "e2e-test-user") };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: JwtIssuer, audience: JwtAudience, claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Task InitializeAsync() => Task.CompletedTask; // the DB is created by the app's startup migration

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Drop the throwaway database, then tear down the host.
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PetShopDbContext>();
            await db.Database.EnsureDeletedAsync();
        }
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var pub = JwtRsa.ExportParameters(includePrivateParameters: false);
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PetShopDb"] = TestConnectionString,
                ["Database:ApplyMigrationsOnStartup"] = "true",
                ["Jwt:Modulus"] = Base64UrlEncoder.Encode(pub.Modulus),
                ["Jwt:Exponent"] = Base64UrlEncoder.Encode(pub.Exponent),
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Diagnostics:LayerTracing:Enabled"] = "false"
            });
        });
    }
}

/// <summary>Shares a single API + database across all e2e test classes.</summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<PetShopApiFactory>
{
    public const string Name = "PetShop API";
}
