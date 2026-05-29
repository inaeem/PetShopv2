using PetShop.Domain.Enums;

namespace PetShop.Domain.Entities;

/// <summary>A pet available in the shop. Maps to dbo.Pets.</summary>
public class Pet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public decimal Price { get; set; }
    public int? AgeMonths { get; set; }
    public PetStatus Status { get; set; } = PetStatus.Available;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}
