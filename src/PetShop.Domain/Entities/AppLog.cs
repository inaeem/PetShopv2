namespace PetShop.Domain.Entities;

/// <summary>
/// One row of the structured application log (dbo.AppLogs).
/// <para>
/// Rows are written by the Serilog MSSqlServer sink, not by EF — the sink inserts them in
/// batches, outside any DbContext or transaction. This entity exists so the application
/// can <em>read</em> its own logs: support screens, "show me everything for this
/// correlation id", per-payer activity, external-API health. Treat it as read-only;
/// saving through it would write rows Serilog never produced.
/// </para>
/// <para>
/// The column names are the app's, not Serilog's: the sink's columnOptions maps each log
/// event property onto the matching column (SourceContext → Source, Elapsed → DurationMs,
/// CorrelationId → CorrelationRef, and so on).
/// </para>
/// </summary>
public class AppLog
{
    public long LogId { get; set; }

    /// <summary>When the event was raised, in UTC.</summary>
    public DateTime LoggedAtUtc { get; set; }

    /// <summary>Serilog level: Verbose, Debug, Information, Warning, Error or Fatal.</summary>
    public string? Severity { get; set; }

    /// <summary>
    /// What kind of row this is: Request, Response, Enter, Exit, ExternalRequest,
    /// ExternalResponse. Null for ordinary business/diagnostic lines.
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>The class that wrote the row (Serilog's SourceContext).</summary>
    public string? Source { get; set; }

    /// <summary>The controller action or class.method being executed when the row was written.</summary>
    public string? Operation { get; set; }

    /// <summary>Rendered message. Also carries the endpoint and the request/response body.</summary>
    public string? LogMessage { get; set; }

    /// <summary>Elapsed milliseconds, for rows that measure something.</summary>
    public double? DurationMs { get; set; }

    /// <summary>HTTP status, for response rows and external-call replies.</summary>
    public int? HttpStatus { get; set; }

    /// <summary>Full exception text, for Error/Fatal rows.</summary>
    public string? ErrorDetail { get; set; }

    /// <summary>Server-generated id shared by every row of one request.</summary>
    public string? CorrelationRef { get; set; }

    // Business identifiers. Null on rows written before the value was known — seeded from
    // the request payload, or set mid-call by the service layer.
    public string? AppRegistrationId { get; set; }
    public string? PayerId { get; set; }
    public string? ProviderId { get; set; }
    public string? UserEmail { get; set; }
}
