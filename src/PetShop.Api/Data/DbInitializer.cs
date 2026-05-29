using Microsoft.EntityFrameworkCore;
using PetShop.Data.Context;

namespace PetShop.Api.Data;

/// <summary>
/// Applies pending migrations on startup. Toggle via the
/// "Database:ApplyMigrationsOnStartup" setting. The API no longer issues tokens or
/// stores users, so there is no user seeding — clients authenticate with an
/// externally-issued JWT.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<PetShopDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        if (config.GetValue("Database:ApplyMigrationsOnStartup", false))
        {
            logger.LogInformation("Applying pending migrations...");
            await db.Database.MigrateAsync();
        }
    }
}
