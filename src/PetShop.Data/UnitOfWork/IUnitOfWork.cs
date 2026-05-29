using PetShop.Data.Repositories;
using PetShop.Domain.Entities;

namespace PetShop.Data.UnitOfWork;

/// <summary>
/// Coordinates repositories over a single DbContext and commits them as one transaction.
/// </summary>
public interface IUnitOfWork
{
    IPetRepository Pets { get; }
    IRepository<Category> Categories { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
