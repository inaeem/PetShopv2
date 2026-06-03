namespace PetShop.Service.External;

/// <summary>
/// Bound from the "PetSync" section of appsettings.json. Configures the external
/// pet service that a newly-created pet is replicated to (best-effort).
/// The external service does authorization only: this app sends a fixed service
/// token (<see cref="ServiceToken"/>) as a bearer token on every request.
/// </summary>
public class PetSyncSettings
{
    public const string SectionName = "PetSync";

    /// <summary>Master switch. When false the remote call is skipped entirely (default).</summary>
    public bool Enabled { get; set; }

    /// <summary>Base URL of the external pet service, e.g. https://pets.example.com.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Relative path of the "create pet" endpoint, appended to <see cref="BaseUrl"/>.</summary>
    public string CreatePetPath { get; set; } = "/pets";

    /// <summary>
    /// Fixed service token sent as <c>Authorization: Bearer &lt;token&gt;</c>. This is a
    /// secret — supply it via env var / user-secrets, never in committed JSON.
    /// </summary>
    public string ServiceToken { get; set; } = string.Empty;

    /// <summary>Per-request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Content-Type sent on the request body. Defaults to JSON.</summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>Accept header advertised on every request. Defaults to JSON.</summary>
    public string Accept { get; set; } = "application/json";

    /// <summary>Extra headers added to every request (header name → value).</summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Client certificates for mutual-TLS, supplied as PFX/PKCS#12 file paths with
    /// an optional password. Loaded onto the HTTP handler at registration time.
    /// </summary>
    public List<ClientCertificateSettings> ClientCertificates { get; set; } = new();

    /// <summary>
    /// When true, the handler sends the Authorization header pre-emptively on the
    /// first request instead of waiting for a 401 challenge.
    /// </summary>
    public bool PreAuthenticate { get; set; }

    /// <summary>Extra attempts after the first on transient failures (5xx / 408 / 429 / network).</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Base backoff between attempts in milliseconds (multiplied by the attempt number).</summary>
    public int RetryBaseDelayMs { get; set; } = 200;
}

/// <summary>A single client certificate for mutual-TLS, loaded from disk.</summary>
public class ClientCertificateSettings
{
    /// <summary>Path to a PFX/PKCS#12 certificate file.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Optional password protecting the certificate file.</summary>
    public string? Password { get; set; }
}
