using System.Diagnostics;
using PetShop.Service.Common;
using Serilog.Context;

namespace PetShop.Api.Middleware;

/// <summary>
/// Starts the per-request log context and gives it a correlation id, so EVERY log line
/// emitted while handling the request — authentication, validation, service and data
/// layers, outbound HttpClient calls, and the final exception handler — carries the same
/// id and lands in the CorrelationRef column of dbo.AppLogs.
/// <para>
/// The id is generated server-side from ASP.NET's own request trace and is never taken
/// from the caller: a client-supplied id can collide between callers, or be forged to
/// bury one request's rows inside another's history. It is still echoed back in the
/// response header so a client or support ticket can quote it.
/// </para>
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // W3C trace id when the request is traced, otherwise Kestrel's per-request id.
        var correlationId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        // Outermost middleware, so the bag exists before anything downstream logs.
        // Business identifiers are filled in later — from the request payload
        // (HttpPayloadLoggingMiddleware) or mid-call by the service layer.
        var fields = RequestLogContext.Begin();
        fields.CorrelationId = correlationId;

        // Echo it back before the response body starts (works even for 401/500).
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Two independent paths carry the id onto log events: this push (read by
        // Enrich.FromLogContext) and RequestFieldsEnricher reading the bag above. Either
        // alone is enough, and they can't disagree — same variable. Keeping both means a
        // missing enricher registration, or code that starts a fresh bag mid-request,
        // can't silently blank out the CorrelationRef column.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
