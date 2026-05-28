namespace PetShop.Service.Security;

/// <summary>Bound from the "Jwt" section of appsettings.json.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Symmetric signing key. Must be at least 32 chars. Keep this in a secret store in production.</summary>
    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;
}
