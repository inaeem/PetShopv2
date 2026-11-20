namespace PetShop.Api.Common;

/// <summary>
/// Bound from the "Cors" section of appsettings.json. Drives the named CORS policy
/// (<see cref="CorsPolicies.Default"/>) applied to the whole pipeline, so what a browser
/// client may do is environment-configurable rather than hard-coded.
/// <para>
/// Every list is a comma-separated string, and every key is optional: a blank or missing
/// value falls back to the permissive default for that aspect ("any" for origins,
/// headers and methods) or is simply skipped where there is no meaningful default
/// (exposed headers, credentials, preflight max-age). "*" is the explicit spelling of
/// the same "any" default.
/// </para>
/// </summary>
public class CorsSettings
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Comma-separated origins permitted to call the API, each a scheme + host + optional
    /// port with no trailing slash
    /// (e.g. "https://app.petshop.example.com,https://admin.petshop.example.com").
    /// "*" — or a blank value — allows any origin, which is appropriate for local
    /// development only.
    /// </summary>
    public string AllowedOrigins { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated request headers the browser may send. "*" — or a blank value —
    /// allows any header.
    /// </summary>
    public string AllowedHeaders { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated HTTP methods the browser may use (e.g. "GET,POST,PUT,DELETE").
    /// "*" — or a blank value — allows any method.
    /// </summary>
    public string AllowedMethods { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated response headers the browser is allowed to read, beyond the
    /// CORS-safelisted set. Include "Token-Expired" where clients need to react to an
    /// expired JWT (see the JwtBearer events in Program.cs). Blank leaves the safelist
    /// as-is.
    /// </summary>
    public string ExposedHeaders { get; set; } = string.Empty;

    /// <summary>
    /// Whether cookies / credentialed requests are allowed. Only applied alongside an
    /// explicit origin list — it cannot be combined with an "*" origin.
    /// </summary>
    public bool AllowCredentials { get; set; }

    /// <summary>
    /// How long (seconds) a browser may cache a preflight response. Unset leaves the
    /// framework default in place.
    /// </summary>
    public int? PreflightMaxAgeSeconds { get; set; }

    /// <summary><see cref="AllowedOrigins"/> split into individual origins.</summary>
    public string[] Origins => Split(AllowedOrigins);

    /// <summary><see cref="AllowedHeaders"/> split into individual header names.</summary>
    public string[] Headers => Split(AllowedHeaders);

    /// <summary><see cref="AllowedMethods"/> split into individual method names.</summary>
    public string[] Methods => Split(AllowedMethods);

    /// <summary><see cref="ExposedHeaders"/> split into individual header names.</summary>
    public string[] Exposed => Split(ExposedHeaders);

    /// <summary>True when origins are unset or the "*" wildcard.</summary>
    public bool AllowsAnyOrigin => IsAnyOrUnset(Origins);

    /// <summary>True when request headers are unset or the "*" wildcard.</summary>
    public bool AllowsAnyHeader => IsAnyOrUnset(Headers);

    /// <summary>True when methods are unset or the "*" wildcard.</summary>
    public bool AllowsAnyMethod => IsAnyOrUnset(Methods);

    /// <summary>
    /// An unset list and an explicit "*" mean the same thing: fall back to allowing any
    /// value for that aspect of the policy.
    /// </summary>
    private static bool IsAnyOrUnset(string[] values) => values.Length == 0 || values.Contains("*");

    private static string[] Split(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
