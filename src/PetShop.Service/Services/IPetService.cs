using PetShop.Service.Common;
using PetShop.Service.DTOs;

namespace PetShop.Service.Services;

public interface IPetService
{
    Task<PagedResult<PetDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PetDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PetSearchResultDto>> SearchAsync(string? term, int? categoryId, CancellationToken ct = default);
    Task<PetDto> CreateAsync(CreatePetRequest request, CancellationToken ct = default);
    Task<PetDto> UpdateAsync(int id, UpdatePetRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
