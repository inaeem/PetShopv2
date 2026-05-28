namespace PetShop.Domain.Entities;

/// <summary>An application user that can authenticate against the API. Maps to dbo.Users.</summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash of the password (never store plaintext).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Comma-separated role list, e.g. "Admin,Manager".</summary>
    public string Roles { get; set; } = "User";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
}
