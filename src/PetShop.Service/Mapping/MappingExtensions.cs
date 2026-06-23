using PetShop.Data.StoredProcedures;
using PetShop.Domain.Entities;
using PetShop.Domain.Enums;
using PetShop.Service.DTOs;

namespace PetShop.Service.Mapping;

/// <summary>
/// Lightweight, explicit entity↔DTO mapping. Kept hand-written to avoid a mapping
/// library dependency and keep the data flow obvious.
/// </summary>
public static class MappingExtensions
{
    public static PetDto ToDto(this Pet pet) => new(
        pet.Id,
        pet.Name,
        pet.Breed,
        pet.Price,
        pet.AgeMonths,
        pet.Status,
        pet.CategoryId,
        pet.Category?.Name);

    public static Pet ToEntity(this CreatePetRequest r) => new()
    {
        Name = r.Name,
        Breed = r.Breed,
        Price = r.Price,
        // Column is non-nullable; absent age defaults to 0 months.
        AgeMonths = r.AgeMonths ?? 0,
        CategoryId = r.CategoryId,
        Status = PetStatus.Available
    };

    public static void Apply(this UpdatePetRequest r, Pet pet)
    {
        pet.Name = r.Name;
        pet.Breed = r.Breed;
        pet.Price = r.Price;
        // Column is non-nullable; absent age defaults to 0 months.
        pet.AgeMonths = r.AgeMonths ?? 0;
        pet.Status = r.Status;
        pet.CategoryId = r.CategoryId;
        pet.UpdatedUtc = DateTime.UtcNow;
    }

    public static PetSearchResultDto ToDto(this PetSearchResult r) => new(
        r.Id,
        r.Name,
        r.Breed,
        r.Price,
        (PetStatus)r.Status,
        r.CategoryName);

    public static CategoryWithPetsDto ToDto(this Category c) => new(
        c.Id,
        c.Name,
        c.Description,
        // The category name on each nested PetDto is left null — it's redundant here,
        // since the parent already carries it (and the back-reference isn't loaded).
        c.Pets.Select(p => p.ToDto()).ToList());
}
