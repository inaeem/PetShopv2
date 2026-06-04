using Microsoft.Extensions.Logging;

namespace PetShop.Service.External;

/// <summary>
/// Pipeline handler that logs the raw PetSync request/response bodies as they
/// cross the wire. Sits between the typed <see cref="PetSyncClient"/> and the
/// primary transport handler, so it sees the fully-serialized HTTP content.
/// Logging is gated on <see cref="LogLevel.Debug"/> so it costs nothing in
/// normal operation.
/// </summary>
public class PetSyncLoggingHandler : DelegatingHandler
{
    private readonly ILogger<PetSyncLoggingHandler> _logger;

    public PetSyncLoggingHandler(ILogger<PetSyncLoggingHandler> logger) => _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (_logger.IsEnabled(LogLevel.Debug) && request.Content is not null)
        {
            // Buffer so the body can be read here and still sent downstream.
            await request.Content.LoadIntoBufferAsync();
            var rawRequest = await request.Content.ReadAsStringAsync(ct);
            _logger.LogDebug("PetSync {Method} {Uri} raw request: {Payload}",
                request.Method, request.RequestUri, rawRequest);
        }

        var response = await base.SendAsync(request, ct);

        if (_logger.IsEnabled(LogLevel.Debug) && response.Content is not null)
        {
            await response.Content.LoadIntoBufferAsync();
            var rawResponse = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug("PetSync {Status} raw response: {Payload}",
                (int)response.StatusCode, rawResponse);
        }

        return response;
    }
}
