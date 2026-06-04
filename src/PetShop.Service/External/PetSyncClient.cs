using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
        // Back-compat: project the remote id (data.id) onto the non-generic result.
        var result = await CreateCoreAsync(pet, TryReadRemoteIdAsync, ct);
        return new PetSyncResult(result.Success, result.Skipped, result.Data, result.Error);
    }

    public Task<PetSyncResult<T>> CreateAsync<T>(Pet pet, CancellationToken ct = default)
        => CreateCoreAsync(pet, TryReadDataAsync<T>, ct);

    /// <summary>
    /// Shared POST + retry pipeline. The success body is handed to <paramref name="readData"/>,
    /// which projects it into whatever <typeparamref name="T"/> the caller wants.
    /// </summary>
    private async Task<PetSyncResult<T>> CreateCoreAsync<T>(
        Pet pet, Func<HttpResponseMessage, CancellationToken, Task<T?>> readData, CancellationToken ct)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("PetSync disabled; skipping remote create for pet {PetId}", pet.Id);
            return PetSyncResult<T>.Disabled();
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
                    var data = await readData(response, ct);
                    _logger.LogInformation(
                        "Synced pet {PetId} to remote service (attempt {Attempt}/{Attempts})",
                        pet.Id, attempt, attempts);
                    return PetSyncResult<T>.Ok(data);
                }

                var status = (int)response.StatusCode;
                var transient = status >= 500 || status == 408 || status == 429;

                if (!transient || attempt == attempts)
                {
                    // Error status (4xx/5xx): the body is a fault, not a T. Status code is
                    // the discriminator, so we read the fault shape rather than guessing.
                    var fault = await TryReadFaultAsync(response, ct);
                    var reason = !string.IsNullOrWhiteSpace(fault?.Message)
                        ? $"HTTP {status}: {fault!.Message}"
                        : $"HTTP {status} {response.ReasonPhrase}";
                    _logger.LogWarning(
                        "Remote pet create failed for pet {PetId}: {Reason} (attempt {Attempt}/{Attempts})",
                        pet.Id, reason, attempt, attempts);
                    return PetSyncResult<T>.Failed(reason, fault);
                }

                var transientReason = $"HTTP {status} {response.ReasonPhrase}";

                _logger.LogWarning(
                    "Remote pet create transient failure for pet {PetId}: {Reason}; retrying (attempt {Attempt}/{Attempts})",
                    pet.Id, transientReason, attempt, attempts);
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
                    return PetSyncResult<T>.Failed(ex.Message);
                }

                _logger.LogWarning(ex,
                    "Remote pet create error for pet {PetId}; retrying (attempt {Attempt}/{Attempts})",
                    pet.Id, attempt, attempts);
            }

            await Task.Delay(_settings.RetryBaseDelayMs * attempt, ct);
        }

        return PetSyncResult<T>.Failed("exhausted retries");
    }

    /// <summary>Reads the remote id from the <c>data.id</c> property of the response.</summary>
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

    /// <summary>
    /// Reads the fault body from an error (4xx/5xx) response. Lenient by design —
    /// only the few guaranteed fields are bound; a missing/non-JSON body yields null.
    /// </summary>
    private async Task<PetSyncFault?> TryReadFaultAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<PetSyncFault>(cancellationToken: ct);
        }
        catch (JsonException)
        {
            // Non-JSON/empty error body — the status code still conveys the failure.
            return null;
        }
    }

    /// <summary>Deserializes the response's <c>data</c> property into the caller's type.</summary>
    private async Task<T?> TryReadDataAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content
                .ReadFromJsonAsync<PetSyncResponseEnvelope<T>>(cancellationToken: ct);
            return body is null ? default : body.Data;
        }
        catch
        {
            // The remote payload is optional; a missing/non-JSON/mismatched body is not a failure.
            return default;
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

    /// <summary>Generic response envelope — binds the "data" object to a caller-supplied type.</summary>
    private record PetSyncResponseEnvelope<T>(
        [property: JsonPropertyName("data")] T? Data);
}
