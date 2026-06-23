namespace PetShop.Data.StoredProcedures;

/// <summary>
/// Keyless projection returned by dbo.usp_AddPlant — the plant row created by the
/// procedure. Mapped via <c>FromSqlInterpolated</c>.
/// </summary>
public class PlantAddResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Species { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedUtc { get; set; }
}
