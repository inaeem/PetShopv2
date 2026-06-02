using PetShop.Service.Common;
using PetShop.Service.DTOs;

namespace PetShop.Service.Services;

public interface IPetService
{
    Task<PagedResult<PetDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PetDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PetSearchResultDto>> SearchAsync(string? term, int? categoryId, CancellationToken ct = default);

    /// <summary>
    /// Returns the authenticated caller's available pets, grouped by category. The caller
    /// is taken from the current user's email claim — no id is passed in.
    /// </summary>
    Task<IReadOnlyList<CategoryWithPetsDto>> GetMyAvailablePetsByCategoryAsync(CancellationToken ct = default);
    Task<PetDto> CreateAsync(CreatePetRequest request, CancellationToken ct = default);
    Task<PetDto> UpdateAsync(int id, UpdatePetRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
