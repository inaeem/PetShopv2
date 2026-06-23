namespace PetShop.Domain.Entities;

/// <summary>A grouping of pets, e.g. Dogs, Cats, Birds. Maps to dbo.Categories.</summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Free-form metadata (e.g. JSON) attached to the category. Optional.</summary>
    public string? Metadata { get; set; }

    public DateTime CreatedUtc { get; set; }

    public ICollection<Pet> Pets { get; set; } = new List<Pet>();
}
