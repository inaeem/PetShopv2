using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetShop.Domain.Entities;

namespace PetShop.Service.External;

/// <summary>
/// Typed <see cref="HttpClient"/> that POSTs a newly-created pet to the external
/// pet service. Best-effort: transient failures are retried with a small backoff,
/// and any unrecoverable transport/HTTP error is returned on the result rather
/// than thrown, so it can never fail the local create.
/// </summary>
public class PetSyncClient : IPetSyncClient
{
    private readonly HttpClient _http;
    private readonly PetSyncSettings _settings;
    private readonly ILogger<PetSyncClient> _logger;

    public PetSyncClient(HttpClient http, IOptions<PetSyncSettings> settings, ILogger<PetSyncClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PetSyncResult> CreateAsync(Pet pet, CancellationToken ct = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("PetSync disabled; skipping remote create for pet {PetId}", pet.Id);
            return PetSyncResult.Disabled();
        }

        var payload = new PetSyncEnvelope(new PetSyncRequest(
            pet.Id, pet.Name, pet.Breed, pet.Price, pet.AgeMonths, pet.CategoryId, pet.Status.ToString()));

        var attempts = Math.Max(1, _settings.MaxRetries + 1);
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var content = JsonContent.Create(payload);
                if (!string.IsNullOrWhiteSpace(_settings.ContentType))
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse(_settings.ContentType);
                using var response = await _http.PostAsync(_settings.CreatePetPath, content, ct);

                if (response.IsSuccessStatusCode)
                {
                    var remoteId = await TryReadRemoteIdAsync(response, ct);
                    _logger.LogInformation(
                        "Synced pet {PetId} to remote service (remoteId={RemoteId}, attempt {Attempt}/{Attempts})",
                        pet.Id, remoteId, attempt, attempts);
                    return PetSyncResult.Ok(remoteId);
                }

                var status = (int)response.StatusCode;
                var reason = $"HTTP {status} {response.ReasonPhrase}";
                var transient = status >= 500 || status == 408 || status == 429;

                if (!transient || attempt == attempts)
                {
                    _logger.LogWarning(
                        "Remote pet create failed for pet {PetId}: {Reason} (attempt {Attempt}/{Attempts})",
                        pet.Id, reason, attempt, attempts);
                    return PetSyncResult.Failed(reason);
                }

                _logger.LogWarning(
                    "Remote pet create transient failure for pet {PetId}: {Reason}; retrying (attempt {Attempt}/{Attempts})",
                    pet.Id, reason, attempt, attempts);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // caller aborted the request — propagate, do not swallow
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // TaskCanceledException here (ct not cancelled) means the HttpClient timeout fired.
                if (attempt == attempts)
                {
                    _logger.LogWarning(ex,
                        "Remote pet create errored for pet {PetId} after {Attempts} attempt(s)", pet.Id, attempts);
                    return PetSyncResult.Failed(ex.Message);
                }

                _logger.LogWarning(ex,
                    "Remote pet create error for pet {PetId}; retrying (attempt {Attempt}/{Attempts})",
                    pet.Id, attempt, attempts);
            }

            await Task.Delay(_settings.RetryBaseDelayMs * attempt, ct);
        }

        return PetSyncResult.Failed("exhausted retries");
    }

    private async Task<string?> TryReadRemoteIdAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<PetSyncResponseEnvelope>(cancellationToken: ct);
            return body?.Data?.Id;
        }
        catch
        {
            // The remote id is optional; a missing/non-JSON body is not a failure.
            return null;
        }
    }

    // ---- Wire contract ----
    // Request : { "pet": { "localId", "name", "breed", "price", "ageMonths", "categoryId", "status" } }
    // Response: { "data": { "id": "<remote id>" } }

    /// <summary>Request envelope — the pet is wrapped under a "pet" property.</summary>
    private record PetSyncEnvelope(
        [property: JsonPropertyName("pet")] PetSyncRequest Pet);

    /// <summary>Pet payload. LocalId lets the remote correlate back to us.</summary>
    private record PetSyncRequest(
        [property: JsonPropertyName("localId")] int LocalId,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("breed")] string? Breed,
        [property: JsonPropertyName("price")] decimal Price,
        [property: JsonPropertyName("ageMonths")] int? AgeMonths,
        [property: JsonPropertyName("categoryId")] int CategoryId,
        [property: JsonPropertyName("status")] string Status);

    /// <summary>Response envelope — the created resource is nested under "data".</summary>
    private record PetSyncResponseEnvelope(
        [property: JsonPropertyName("data")] PetSyncResponseData? Data);

    private record PetSyncResponseData(
        [property: JsonPropertyName("id")] string? Id);
}
