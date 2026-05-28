using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PetShopDb"] = TestConnectionString,
                ["Database:ApplyMigrationsOnStartup"] = "true",
                ["Database:SeedAdmin"] = "true",
                ["Database:AdminPassword"] = "Admin#12345",
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
