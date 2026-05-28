using Microsoft.EntityFrameworkCore;
using PetShop.Data.Context;
using PetShop.Domain.Entities;
using PetShop.Service.Security;

namespace PetShop.Api.Data;

/// <summary>
/// Applies pending migrations and seeds a default admin user on startup.
/// Toggle via the "Database:ApplyMigrationsOnStartup" / "Database:SeedAdmin" settings.
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

        if (config.GetValue("Database:SeedAdmin", true) && !await db.Users.AnyAsync())
        {
            var hasher = sp.GetRequiredService<IPasswordHasher>();
            db.Users.Add(new User
            {
                Username = "admin",
                Email = "admin@petshop.local",
                PasswordHash = hasher.Hash(config["Database:AdminPassword"] ?? "Admin#12345"),
                Roles = "Admin,User",
                IsActive = true
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded default admin user.");
        }
    }
}
