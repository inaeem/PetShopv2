namespace PetShop.Service.Security;

/// <summary>
/// Bound from the "Jwt" section of appsettings.json. Used to <b>validate</b> incoming
/// bearer tokens (the API no longer issues them — clients send an externally-obtained
/// JWT). The token must be signed by the issuer's RSA private key; the API verifies the
/// signature with the issuer's RSA public key, supplied here as its <see cref="Modulus"/>
/// and <see cref="Exponent"/>, and the token must carry the expected
/// <see cref="Issuer"/>/<see cref="Audience"/>.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// RSA public-key modulus ("n"), base64url-encoded (JWK form). Combined with
    /// <see cref="Exponent"/> to verify the token signature.
    /// </summary>
    public string Modulus { get; set; } = string.Empty;

    /// <summary>
    /// RSA public-key exponent ("e"), base64url-encoded (JWK form) — typically "AQAB".
    /// </summary>
    public string Exponent { get; set; } = string.Empty;
}
