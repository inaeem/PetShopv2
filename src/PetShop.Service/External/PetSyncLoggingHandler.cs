using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PetShop.Service.Common;

namespace PetShop.Service.External;

/// <summary>
/// Pipeline handler that logs the raw PetSync request/response bodies as they
/// cross the wire. Sits between the typed <see cref="PetSyncClient"/> and the
/// primary transport handler, so it sees the fully-serialized HTTP content.
/// <para>
/// Logged at Information (not Debug) because an external integration is exactly what you
/// need in Production when a partner disputes what was sent; a non-2xx reply is logged as
/// a Warning. Rows are tagged ExternalRequest / ExternalResponse and inherit the calling
/// request's correlation id and business identifiers from the ambient log context, so an
/// outbound call sits in the table alongside the inbound request that caused it.
/// </para>
/// <para>
/// The correlation id is also forwarded as a header, so the remote side's logs can be
/// lined up with ours. It is read from <see cref="RequestLogContext"/> rather than
/// injected: a DelegatingHandler outlives a request (handlers are pooled for minutes), so
/// a scoped dependency here would capture one caller's identity and reuse it for everyone.
/// </para>
/// </summary>
public class PetSyncLoggingHandler : DelegatingHandler
{
    public const string CorrelationHeaderName = "X-Correlation-ID";

    private readonly ILogger<PetSyncLoggingHandler> _logger;

    public PetSyncLoggingHandler(ILogger<PetSyncLoggingHandler> logger) => _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var correlationId = RequestLogContext.Current?.CorrelationId;
        if (!string.IsNullOrEmpty(correlationId))
            request.Headers.TryAddWithoutValidation(CorrelationHeaderName, correlationId);

        var requestBody = "";
        if (request.Content is not null)
        {
            // Buffer so the body can be read here and still sent downstream.
            await request.Content.LoadIntoBufferAsync();
            requestBody = await request.Content.ReadAsStringAsync(ct);
        }

        using (_logger.BeginScope(EventType("ExternalRequest")))
            _logger.LogInformation("→ PetSync {Method} {Uri} {Body:l}",
                request.Method, request.RequestUri, requestBody);

        var start = Stopwatch.GetTimestamp();
        var response = await base.SendAsync(request, ct);
        var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

        var responseBody = "";
        if (response.Content is not null)
        {
            await response.Content.LoadIntoBufferAsync();
            responseBody = await response.Content.ReadAsStringAsync(ct);
        }

        using (_logger.BeginScope(EventType("ExternalResponse")))
            _logger.Log(response.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning,
                "← PetSync {StatusCode} {Uri} in {Elapsed:0.0} ms {Body:l}",
                (int)response.StatusCode, request.RequestUri, elapsedMs, responseBody);

        return response;
    }

    private static Dictionary<string, object> EventType(string eventType) =>
        new() { ["EventType"] = eventType };
}
