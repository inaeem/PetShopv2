namespace PetShop.Domain.Entities;

/// <summary>A plant available in the shop. Maps to dbo.Plants.</summary>
public class Plant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Species { get; set; }
    public decimal Price { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}
