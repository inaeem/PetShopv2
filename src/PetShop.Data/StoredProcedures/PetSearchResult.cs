namespace PetShop.Data.StoredProcedures;

/// <summary>
/// Keyless projection returned by dbo.usp_SearchPets. Mapped via
/// <c>FromSqlInterpolated</c> in <see cref="Repositories.PetRepository"/>.
/// </summary>
public class PetSearchResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public decimal Price { get; set; }
    public int Status { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
