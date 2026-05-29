using Microsoft.EntityFrameworkCore;
using PetShop.Data.StoredProcedures;
using PetShop.Domain.Entities;

namespace PetShop.Data.Context;

/// <summary>
/// EF Core context for the pet shop database. This is a database-first model:
/// the entities map onto an existing schema, and migrations are used to keep the
/// schema in sync (see <c>Migrations/</c>). The context also exposes keyless
/// result sets so stored procedures can be invoked directly.
/// </summary>
public class PetShopDbContext : DbContext
{
    public PetShopDbContext(DbContextOptions<PetShopDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Pet> Pets => Set<Pet>();

    /// <summary>Keyless result set returned by the dbo.usp_SearchPets stored procedure.</summary>
    public DbSet<PetSearchResult> PetSearchResults => Set<PetSearchResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> implementations in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PetShopDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
