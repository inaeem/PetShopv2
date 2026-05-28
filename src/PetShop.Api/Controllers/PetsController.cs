using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetShop.Service.Common;
using PetShop.Service.DTOs;
using PetShop.Service.Services;

namespace PetShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PetsController : ControllerBase
{
    private readonly IPetService _pets;

    public PetsController(IPetService pets) => _pets = pets;

    /// <summary>Returns a paged list of pets.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _pets.GetPagedAsync(page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<PetDto>>.Ok(result));
    }

    /// <summary>Searches pets via the dbo.usp_SearchPets stored procedure.</summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string? term, [FromQuery] int? categoryId, CancellationToken ct = default)
    {
        var result = await _pets.SearchAsync(term, categoryId, ct);
        return Ok(ApiResponse<IReadOnlyList<PetSearchResultDto>>.Ok(result));
    }

    /// <summary>Returns a single pet by id.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var pet = await _pets.GetByIdAsync(id, ct);
        return Ok(ApiResponse<PetDto>.Ok(pet));
    }

    /// <summary>Creates a new pet. Requires an authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PetDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePetRequest request, CancellationToken ct)
    {
        var pet = await _pets.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = pet.Id },
            ApiResponse<PetDto>.Ok(pet, "Pet created."));
    }

    /// <summary>Updates an existing pet.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePetRequest request, CancellationToken ct)
    {
        var pet = await _pets.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<PetDto>.Ok(pet, "Pet updated."));
    }

    /// <summary>Deletes a pet. Requires the Admin role.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _pets.DeleteAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Pet deleted."));
    }
}
