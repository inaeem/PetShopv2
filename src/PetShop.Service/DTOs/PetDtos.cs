using PetShop.Domain.Enums;

namespace PetShop.Service.DTOs;

public record PetDto(
    int Id,
    string Name,
    string? Breed,
    decimal Price,
    int? AgeMonths,
    PetStatus Status,
    int CategoryId,
    string? CategoryName);

public record CreatePetRequest(
    string Name,
    string? Breed,
    decimal Price,
    int? AgeMonths,
    int CategoryId);

public record UpdatePetRequest(
    string Name,
    string? Breed,
    decimal Price,
    int? AgeMonths,
    PetStatus Status,
    int CategoryId);

public record PetSearchResultDto(
    int Id,
    string Name,
    string? Breed,
    decimal Price,
    PetStatus Status,
    string CategoryName);
