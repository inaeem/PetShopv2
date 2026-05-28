namespace PetShop.Domain.Enums;

/// <summary>Lifecycle state of a customer order.</summary>
public enum OrderStatus
{
    Placed = 0,
    Paid = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
