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
        IUserRepository users,
        IRepository<Category> categories,
        IRepository<Order> orders)
    {
        _db = db;
        Pets = pets;
        Users = users;
        Categories = categories;
        Orders = orders;
    }

    public IPetRepository Pets { get; }
    public IUserRepository Users { get; }
    public IRepository<Category> Categories { get; }
    public IRepository<Order> Orders { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
