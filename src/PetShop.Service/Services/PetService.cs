using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PetShop.Data.Diagnostics;
using PetShop.Data.UnitOfWork;
using PetShop.Service.Common;
using PetShop.Service.DTOs;
using PetShop.Service.External;
using PetShop.Service.Mapping;

namespace PetShop.Service.Services;

public class PetService : IPetService
{
    private const string Category = "PetShop.Service.PetService";

    private readonly IUnitOfWork _uow;
    private readonly ILogger<PetService> _logger;
    private readonly ILayerTracer _tracer;
    private readonly IPetSyncClient _petSync;

    public PetService(IUnitOfWork uow, ILogger<PetService> logger, ILayerTracer tracer, IPetSyncClient petSync)
    {
        _uow = uow;
        _logger = logger;
        _tracer = tracer;
        _petSync = petSync;
    }

    private Task<T> Measure<T>(string member, object? args, Func<Task<T>> body)
        => _tracer.MeasureAsync(LayerKind.Service, Category, member, body, args);

    private Task Measure(string member, object? args, Func<Task> body)
        => _tracer.MeasureAsync(LayerKind.Service, Category, member, body, args);

    public Task<PagedResult<PetDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        => Measure(nameof(GetPagedAsync), new { page, pageSize }, async () =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            var query = _uow.Pets.Query()
                .AsNoTracking()
                .Include(p => p.Category)
                .OrderBy(p => p.Id);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<PetDto>
            {
                Items = items.Select(p => p.ToDto()).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        });

    public Task<PetDto> GetByIdAsync(int id, CancellationToken ct = default)
        => Measure(nameof(GetByIdAsync), new { id }, async () =>
        {
            var pet = await _uow.Pets.GetWithCategoryAsync(id, ct)
                ?? throw AppException.NotFound("Pet");
            return pet.ToDto();
        });

    public Task<IReadOnlyList<PetSearchResultDto>> SearchAsync(string? term, int? categoryId, CancellationToken ct = default)
        => Measure<IReadOnlyList<PetSearchResultDto>>(nameof(SearchAsync), new { term, categoryId }, async () =>
        {
            // Goes through the stored procedure in the data layer.
            var results = await _uow.Pets.SearchAsync(term, categoryId, ct);
            return results.Select(r => r.ToDto()).ToList();
        });

    public Task<PetDto> CreateAsync(CreatePetRequest request, CancellationToken ct = default)
        => Measure(nameof(CreateAsync), new { request.Name, request.CategoryId }, async () =>
        {
            var category = await _uow.Categories.GetByIdAsync(request.CategoryId, ct)
                ?? throw AppException.NotFound("Category");

            var pet = request.ToEntity();
            await _uow.Pets.AddAsync(pet, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Created pet {PetId} ({PetName})", pet.Id, pet.Name);

            // Step 2 of the create flow: replicate to the external pet service.
            // Best-effort — the pet is already persisted locally, so a remote
            // failure is logged but does NOT fail the create (the client never
            // throws for transport/HTTP errors).
            var sync = await _petSync.CreateAsync(pet, ct);
            if (!sync.Success && !sync.Skipped)
                _logger.LogWarning(
                    "Pet {PetId} created locally but remote sync failed: {Error}", pet.Id, sync.Error);

            pet.Category = category;
            return pet.ToDto();
        });

    public Task<PetDto> UpdateAsync(int id, UpdatePetRequest request, CancellationToken ct = default)
        => Measure(nameof(UpdateAsync), new { id, request.CategoryId }, async () =>
        {
            var pet = await _uow.Pets.GetByIdAsync(id, ct)
                ?? throw AppException.NotFound("Pet");

            if (await _uow.Categories.GetByIdAsync(request.CategoryId, ct) is null)
                throw AppException.NotFound("Category");

            request.Apply(pet);
            _uow.Pets.Update(pet);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Updated pet {PetId}", pet.Id);
            return (await _uow.Pets.GetWithCategoryAsync(id, ct))!.ToDto();
        });

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => Measure(nameof(DeleteAsync), new { id }, async () =>
        {
            var pet = await _uow.Pets.GetByIdAsync(id, ct)
                ?? throw AppException.NotFound("Pet");
            _uow.Pets.Remove(pet);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Deleted pet {PetId}", id);
        });
}
