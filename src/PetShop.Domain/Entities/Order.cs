using PetShop.Domain.Enums;

namespace PetShop.Domain.Entities;

/// <summary>A customer order. Maps to dbo.Orders.</summary>
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public decimal TotalAmount { get; set; }
    public DateTime OrderedUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
