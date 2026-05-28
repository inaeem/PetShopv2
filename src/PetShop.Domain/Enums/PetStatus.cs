namespace PetShop.Domain.Enums;

/// <summary>Lifecycle state of a pet listing in the shop.</summary>
public enum PetStatus
{
    Available = 0,
    Pending = 1,
    Sold = 2,
    Unavailable = 3
}
