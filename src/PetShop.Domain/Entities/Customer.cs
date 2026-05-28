namespace PetShop.Domain.Entities;

/// <summary>A purchasing customer. Maps to dbo.Customers.</summary>
public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedUtc { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
