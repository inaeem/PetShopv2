using Microsoft.EntityFrameworkCore;
using PetShop.Data.Context;
using PetShop.Data.Diagnostics;
using PetShop.Data.StoredProcedures;
using PetShop.Domain.Entities;
using PetShop.Domain.Enums;

namespace PetShop.Data.Repositories;

public class PetRepository : Repository<Pet>, IPetRepository
{
    public PetRepository(PetShopDbContext db, ILayerTracer tracer) : base(db, tracer) { }

    public Task<Pet?> GetWithCategoryAsync(int id, CancellationToken ct = default)
        => Measure(nameof(GetWithCategoryAsync),
            async () => await Set.AsNoTracking()
                                 .Include(p => p.Category)
                                 .FirstOrDefaultAsync(p => p.Id == id, ct),
            new { id });

    public Task<IReadOnlyList<PetSearchResult>> SearchAsync(string? term, int? categoryId, CancellationToken ct = default)
        => Measure<IReadOnlyList<PetSearchResult>>(nameof(SearchAsync),
            // Direct stored-procedure invocation. FromSqlInterpolated parameterises the
            // values, so this is not vulnerable to SQL injection.
            async () => await Db.PetSearchResults
                .FromSqlInterpolated($"EXEC dbo.usp_SearchPets @Term = {term}, @CategoryId = {categoryId}")
                .AsNoTracking()
                .ToListAsync(ct),
            new { term, categoryId });

    public Task<IReadOnlyList<Category>> GetCategoriesWithAvailablePetsForOwnerAsync(
        string ownerEmail, CancellationToken ct = default)
        => Measure<IReadOnlyList<Category>>(nameof(GetCategoriesWithAvailablePetsForOwnerAsync),
            async () => await Db.Categories.AsNoTracking()
                // Keep only categories that have a matching pet...
                .Where(c => c.Pets.Any(p => p.Status == PetStatus.Available && p.OwnerEmail == ownerEmail))
                // ...and within each, include ONLY those pets (filtered Include). The
                // captured ownerEmail is parameterised by EF, so this is injection-safe.
                .Include(c => c.Pets.Where(p => p.Status == PetStatus.Available && p.OwnerEmail == ownerEmail))
                .ToListAsync(ct),
            new { ownerEmail });
}
