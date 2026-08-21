using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Controllers;
using PetShop.Service.Common;
using Serilog.Context;

namespace PetShop.Api.Middleware;

/// <summary>
/// Logs one row for the inbound request and one for the outbound response, each carrying
/// the body inside the message, and seeds the request's business identifiers
/// (AppRegistrationId / PayerId / ProviderId) from the request payload.
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

    /// <summary>Request-payload properties that seed the log columns of the same name.</summary>
    private static readonly string[] PayloadFields = { "appRegistrationId", "payerId", "providerId" };

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

        var operation = $"{descriptor.ControllerName}Controller.{descriptor.ActionName}";
        var path = context.Request.Path.ToString();
        var requestBody = await ReadRequestBodyAsync(context.Request);

        SeedBusinessFields(requestBody);

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
    /// Pulls AppRegistrationId / PayerId / ProviderId out of the request payload (top-level,
    /// case-insensitive) into the request's log context. Anything absent here can still be
    /// set later by the service layer via <see cref="RequestLogContext.Set"/>.
    /// </summary>
    private static void SeedBusinessFields(string requestBody)
    {
        if (string.IsNullOrWhiteSpace(requestBody)) return;

        try
        {
            using var document = JsonDocument.Parse(requestBody);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return;

            foreach (var property in document.RootElement.EnumerateObject())
            {
                var match = PayloadFields.FirstOrDefault(f =>
                    string.Equals(f, property.Name, StringComparison.OrdinalIgnoreCase));
                if (match is null) continue;

                var value = property.Value.ToString();
                if (string.IsNullOrWhiteSpace(value)) continue;

                RequestLogContext.Set(fields =>
                {
                    switch (match)
                    {
                        case "appRegistrationId": fields.AppRegistrationId = value; break;
                        case "payerId": fields.PayerId = value; break;
                        case "providerId": fields.ProviderId = value; break;
                    }
                });
            }
        }
        catch (JsonException)
        {
            // Malformed or non-object payload: nothing to seed, and never a reason to fail the request.
        }
    }

    private static string BodyForLog(string path, string body) =>
        RedactedPaths.Any(p => path.Contains(p, StringComparison.OrdinalIgnoreCase))
            ? "[redacted]"
            : Truncate(body);

    private static string Truncate(string body) =>
        body.Length <= MaxBodyChars ? body : body[..MaxBodyChars] + "…[truncated]";
}
