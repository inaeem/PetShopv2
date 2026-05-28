using Serilog.Context;

namespace PetShop.Api.Middleware;

/// <summary>
/// Establishes a correlation id for the request (honouring an inbound
/// X-Correlation-ID header, otherwise the framework trace id) and pushes it into
/// the Serilog LogContext so EVERY log line emitted while handling the request —
/// authentication, validation, service and data layers, and the final exception
/// handler — carries the same id. The id is also echoed back in the response
/// header so a client/support ticket can pinpoint the exact request.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Request.Headers.TryGetValue(HeaderName, out var inbound) && !string.IsNullOrWhiteSpace(inbound)
                ? inbound.ToString()
                : context.TraceIdentifier;

        // Echo it back before the response body starts (works even for 401/500).
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Every ILogger call nested under this scope is enriched with CorrelationId.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
