using PetShop.Data.StoredProcedures;
using PetShop.Domain.Entities;

namespace PetShop.Data.Repositories;

public interface IPetRepository : IRepository<Pet>
{
    /// <summary>Loads a pet together with its category in a single round-trip.</summary>
    Task<Pet?> GetWithCategoryAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Invokes the dbo.usp_SearchPets stored procedure directly. Demonstrates the
    /// data layer calling a SQL Server procedure rather than going through the entity model.
    /// </summary>
    Task<IReadOnlyList<PetSearchResult>> SearchAsync(string? term, int? categoryId, CancellationToken ct = default);
}
