using System.Threading;

namespace PetShop.Service.Common;

/// <summary>
/// Per-request bag of business identifiers that every log row should carry
/// (correlation id, AppRegistrationId, PayerId, ProviderId, UserEmail).
/// <para>
/// Why a mutable ambient object rather than <c>LogContext.PushProperty</c>: some of these
/// values are only known part-way through the call — a payer resolved in the service
/// layer, a provider looked up in the database — and wrapping that discovery point in a
/// <c>using</c> block is not practical. The bag is created once per request by
/// <c>CorrelationIdMiddleware</c>; the reference then flows down the async call tree, so
/// a field set anywhere is visible to every log line written afterwards, at any depth.
/// A Serilog enricher (<c>RequestFieldsEnricher</c>) reads it at write time.
/// </para>
/// <para>
/// Deliberately static rather than DI-scoped: <c>DelegatingHandler</c>s live for the
/// HttpClient handler lifetime (~2 minutes), so injecting a scoped service into one
/// captures a single request's values for every later request.
/// </para>
/// </summary>
public sealed class RequestLogContext
{
    private static readonly AsyncLocal<RequestLogContext?> Ambient = new();

    /// <summary>The current request's bag, or null outside a request (startup, background work).</summary>
    public static RequestLogContext? Current => Ambient.Value;

    /// <summary>Starts a new bag for this request. Call once, as early in the pipeline as possible.</summary>
    public static RequestLogContext Begin()
    {
        var context = new RequestLogContext();
        Ambient.Value = context;
        return context;
    }

    /// <summary>
    /// Sets fields on the current bag if there is one. Safe to call from anywhere —
    /// a no-op outside a request instead of a null reference.
    /// </summary>
    public static void Set(Action<RequestLogContext> mutate)
    {
        if (Ambient.Value is { } context) mutate(context);
    }

    /// <summary>Server-generated id shared by every log row of one request.</summary>
    public string? CorrelationId { get; set; }

    public string? AppRegistrationId { get; set; }
    public string? PayerId { get; set; }
    public string? ProviderId { get; set; }
    public string? UserEmail { get; set; }
}
