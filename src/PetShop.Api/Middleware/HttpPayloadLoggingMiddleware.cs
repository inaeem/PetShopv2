using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Controllers;
using PetShop.Service.Common;
using Serilog.Context;

namespace PetShop.Api.Middleware;

/// <summary>
/// Logs one row for the inbound request and one for the outbound response, each carrying
/// the body inside the message, and seeds the request's business identifiers
/// (AppRegistrationId / PayerId / ProviderId) from the URL — route values first, then the
/// query string, then the body.
/// <para>
/// Only runs for controller endpoints — static files, Swagger and the messaging page are
/// skipped, so their bodies are never buffered. Bodies are truncated, and requests to
/// credential endpoints are redacted rather than logged.
/// </para>
/// <para>
/// Sits inside <c>UseRouting</c> (so the controller/action is already known) and outside
/// <c>ExceptionHandlingMiddleware</c> (so the response row sees the real status code and
/// error body of a failed request, not the exception on its way up).
/// </para>
/// </summary>
public class HttpPayloadLoggingMiddleware
{
    /// <summary>Bodies longer than this are truncated — a log row is not an archive.</summary>
    private const int MaxBodyChars = 4000;

    /// <summary>Request bodies on these paths are never logged (credentials, tokens).</summary>
    private static readonly string[] RedactedPaths = { "/auth", "/token", "/login", "/password" };

    /// <summary>
    /// Business identifiers to lift out of the request, and the names they travel under.
    /// Most arrive as route segments (/appregistrations/{appRegistrationId}/payers/{payerId}/…),
    /// some as query parameters, some in the body — so all three are searched, in that order
    /// of authority. Add an alias here when an endpoint names a segment differently.
    /// </summary>
    private static readonly (string Field, string[] Aliases)[] BusinessFields =
    {
        (nameof(RequestLogContext.AppRegistrationId),
            new[] { "appRegistrationId", "appRegId", "registrationId" }),
        (nameof(RequestLogContext.PayerId),
            new[] { "payerId", "payer" }),
        (nameof(RequestLogContext.ProviderId),
            new[] { "providerId", "provider" })
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<HttpPayloadLoggingMiddleware> _logger;

    public HttpPayloadLoggingMiddleware(RequestDelegate next, ILogger<HttpPayloadLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var descriptor = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (descriptor is null)
        {
            await _next(context);   // not an API call: static file, swagger, unmatched route
            return;
        }

        EnsureCorrelation(context);

        var operation = $"{descriptor.ControllerName}Controller.{descriptor.ActionName}";
        var path = context.Request.Path.ToString();
        var requestBody = await ReadRequestBodyAsync(context.Request);

        SeedBusinessFields(context.Request, requestBody);

        // Operation covers every log line of this request until a deeper layer (LayerTracer)
        // pushes its own; the innermost push wins, then reverts on exit.
        using (LogContext.PushProperty("Operation", operation))
        {
            using (LogContext.PushProperty("EventType", "Request"))
                _logger.LogInformation("→ {Method} {Path} {Body:l}",
                    context.Request.Method, path, BodyForLog(path, requestBody));

            var originalBody = context.Response.Body;
            using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await _next(context);

                buffer.Position = 0;
                var responseBody = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody);

                using (LogContext.PushProperty("EventType", "Response"))
                    _logger.LogInformation("← {StatusCode} {Path} {Body:l}",
                        context.Response.StatusCode, path, Truncate(responseBody));
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }
    }

    /// <summary>
    /// Guarantees this request has a log context with a correlation id, so the Request and
    /// Response rows can never come out with a NULL CorrelationRef.
    /// <para>
    /// Normally <c>CorrelationIdMiddleware</c> has already done this and both calls below
    /// are no-ops. The fallback covers the case where this middleware ends up ahead of it
    /// in the pipeline — a pipeline order that is easy to change and produces no error,
    /// just orphaned rows. Deriving the id the same way keeps it identical either way.
    /// </para>
    /// </summary>
    private static void EnsureCorrelation(HttpContext context)
    {
        var fields = RequestLogContext.Current ?? RequestLogContext.Begin();
        fields.CorrelationId ??= Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }

    /// <summary>
    /// Reads the request body without consuming it: buffering lets model binding read the
    /// same stream afterwards. Non-JSON and empty bodies are skipped.
    /// </summary>
    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0) return "";
        if (request.ContentType is null || !request.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return "";

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    /// <summary>
    /// Lifts AppRegistrationId / PayerId / ProviderId out of the request into the log
    /// context, so every row of this request carries them in their own columns.
    /// <para>
    /// Looked up in order: route values (the usual home — they are part of the resource
    /// path), then the query string, then the top-level JSON body. First non-empty hit
    /// wins; the route is checked first because a path segment identifies the resource
    /// being acted on, while a body field is just data being sent. Whatever is absent here
    /// can still be set later by the service layer via <see cref="RequestLogContext.Set"/>.
    /// </para>
    /// <para>
    /// Route values require this middleware to run after UseRouting — see Program.cs.
    /// </para>
    /// </summary>
    private static void SeedBusinessFields(HttpRequest request, string requestBody)
    {
        var bodyValues = ParseBodyValues(requestBody);

        foreach (var (field, aliases) in BusinessFields)
        {
            string? value = null;

            foreach (var alias in aliases)
            {
                value = FromRoute(request, alias)
                        ?? FromQuery(request, alias)
                        ?? FromBody(bodyValues, alias);

                if (!string.IsNullOrWhiteSpace(value)) break;
            }

            if (string.IsNullOrWhiteSpace(value)) continue;

            var found = value;
            RequestLogContext.Set(fields => Assign(fields, field, found));
        }
    }

    private static string? FromRoute(HttpRequest request, string name) =>
        request.RouteValues.TryGetValue(name, out var value) ? value?.ToString() : null;

    private static string? FromQuery(HttpRequest request, string name) =>
        request.Query.TryGetValue(name, out var value) ? value.ToString() : null;

    private static string? FromBody(Dictionary<string, string> bodyValues, string name) =>
        bodyValues.TryGetValue(name, out var value) ? value : null;

    /// <summary>
    /// Flattens the top level of a JSON body into a case-insensitive lookup. Nested objects
    /// and arrays are skipped: an identifier buried in a sub-object is a data field, not the
    /// subject of the call, and guessing at it would produce misleading columns.
    /// </summary>
    private static Dictionary<string, string> ParseBodyValues(string requestBody)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(requestBody)) return values;

        try
        {
            using var document = JsonDocument.Parse(requestBody);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return values;

            foreach (var property in document.RootElement.EnumerateObject())
                if (property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                    values[property.Name] = property.Value.ToString();
        }
        catch (JsonException)
        {
            // Malformed payload: nothing to seed, and never a reason to fail the request.
        }

        return values;
    }

    private static void Assign(RequestLogContext fields, string field, string value)
    {
        switch (field)
        {
            case nameof(RequestLogContext.AppRegistrationId): fields.AppRegistrationId ??= value; break;
            case nameof(RequestLogContext.PayerId): fields.PayerId ??= value; break;
            case nameof(RequestLogContext.ProviderId): fields.ProviderId ??= value; break;
        }
    }

    private static string BodyForLog(string path, string body) =>
        RedactedPaths.Any(p => path.Contains(p, StringComparison.OrdinalIgnoreCase))
            ? "[redacted]"
            : Truncate(body);

    private static string Truncate(string body) =>
        body.Length <= MaxBodyChars ? body : body[..MaxBodyChars] + "…[truncated]";
}
