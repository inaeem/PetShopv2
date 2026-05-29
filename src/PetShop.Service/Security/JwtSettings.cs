namespace PetShop.Service.Security;

/// <summary>
/// Bound from the "Jwt" section of appsettings.json. Used to <b>validate</b> incoming
/// bearer tokens (the API no longer issues them — clients send an externally-obtained
/// JWT). The token must be signed with this shared symmetric <see cref="Key"/> and
/// carry the expected <see cref="Issuer"/>/<see cref="Audience"/>.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Symmetric signing key. Must be at least 32 chars. Keep this in a secret store in production.</summary>
    public string Key { get; set; } = string.Empty;
}
