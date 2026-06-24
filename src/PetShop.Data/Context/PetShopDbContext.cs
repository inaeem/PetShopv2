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
    public DbSet<Plant> Plants => Set<Plant>();

    /// <summary>Usage groupings; each row links to either a Pet or a Plant.</summary>
    public DbSet<UsesGroup> UsesGroups => Set<UsesGroup>();

    /// <summary>Activity/audit log; each row links to exactly one Pet, Plant, or Category.</summary>
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    /// <summary>Keyless result set returned by the dbo.usp_SearchPets stored procedure.</summary>
    public DbSet<PetSearchResult> PetSearchResults => Set<PetSearchResult>();

    /// <summary>Keyless result set (the created plant) returned by the dbo.usp_AddPlant stored procedure.</summary>
    public DbSet<PlantAddResult> PlantAddResults => Set<PlantAddResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> implementations in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PetShopDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
