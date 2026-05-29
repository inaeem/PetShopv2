using PetShop.Data.Context;
using PetShop.Data.Repositories;
using PetShop.Domain.Entities;

namespace PetShop.Data.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly PetShopDbContext _db;

    // Repositories are injected (not new-ed) so cross-cutting dependencies such as
    // the layer tracer flow in via DI. All share the same scoped DbContext.
    public UnitOfWork(
        PetShopDbContext db,
        IPetRepository pets,
        IRepository<Category> categories)
    {
        _db = db;
        Pets = pets;
        Categories = categories;
    }

    public IPetRepository Pets { get; }
    public IRepository<Category> Categories { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
