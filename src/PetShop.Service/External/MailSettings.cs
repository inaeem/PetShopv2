namespace PetShop.Service.External;

/// <summary>
/// Bound from the "Mail" section of appsettings.json. Configures the SMTP server used to
/// send outbound email. Like <see cref="PetSyncSettings"/>, sending is gated by
/// <see cref="Enabled"/> (off by default) so non-configured environments make no SMTP
/// calls. <see cref="Password"/> is a secret — supply it via env var / user-secrets,
/// never in committed JSON.
/// </summary>
public class MailSettings
{
    public const string SectionName = "Mail";

    /// <summary>Master switch. When false, sends are skipped entirely (default).</summary>
    public bool Enabled { get; set; }

    /// <summary>SMTP host, e.g. smtp.example.com.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP port. 587 (STARTTLS) is the common default.</summary>
    public int Port { get; set; } = 587;

    /// <summary>Whether to use SSL/TLS for the connection.</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>SMTP username. Leave blank for an unauthenticated relay.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP password — a secret; supply via env var / user-secrets.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>The From address on outbound mail.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>The From display name on outbound mail.</summary>
    public string FromName { get; set; } = "PetShop";

    /// <summary>Per-send timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Root folder holding email templates, one sub-folder per template (each with a
    /// <c>subject.txt</c> and a <c>body.html</c>/<c>body.txt</c>). A relative path is
    /// resolved against the app base directory; an absolute path is used as-is.
    /// </summary>
    public string TemplatesPath { get; set; } = "EmailTemplates";
}
