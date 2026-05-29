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

    /// <summary>Extra attempts after the first on transient failures (5xx / 408 / 429 / network).</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Base backoff between attempts in milliseconds (multiplied by the attempt number).</summary>
    public int RetryBaseDelayMs { get; set; } = 200;
}
