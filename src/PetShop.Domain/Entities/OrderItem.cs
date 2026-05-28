namespace PetShop.Domain.Entities;

/// <summary>A line item within an order. Maps to dbo.OrderItems.</summary>
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int PetId { get; set; }
    public Pet? Pet { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
