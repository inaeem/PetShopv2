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
/// pet service. Transient failures are retried with a small backoff; a remote that
/// answers unusably — a non-success status, or a success with an empty/unreadable
/// body — throws. Transport failures (connect errors, timeouts) are still reported
/// on the result. Callers that must not fail for a remote problem catch around the
/// call; see <see cref="Services.PetService"/>.
/// </summary>
public class PetSyncClient : IPetSyncClient
{
    /// <summary>camelCase, case-insensitive binding — the defaults <c>ReadFromJsonAsync</c> uses.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Plain-text rejections the remote answers with a 2xx status instead of an error code.
    /// They are not JSON, so deserializing one yields an empty payload and the rejection is
    /// lost — the success body is matched against these as text before anything is parsed.
    /// Matched case-insensitively on a substring, since the id is appended to the message.
    /// </summary>
    private static readonly string[] RemoteTextRejections =
    {
        "Can not create duplicate client with ID",
        "Cannot create duplicate client with ID",
    };

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
        // Back-compat: the remote's response body is the remote id.
        var result = await CreateCoreAsync(pet, ReadBodyAsync, ct);
        return new PetSyncResult(result.Success, result.Skipped, result.Data, result.Error);
    }

    public Task<PetSyncResult<T>> CreateAsync<T>(Pet pet, CancellationToken ct = default)
        => CreateCoreAsync(pet, ReadTypedBodyAsync<T>, ct);

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
                    throw new InvalidOperationException(
                        $"Remote pet create failed for pet {pet.Id}: {reason}");
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

    /// <summary>
    /// Reads the success body as text — no JSON parsing, no envelope digging, so the
    /// caller gets exactly what the remote sent. Two bodies are rejected before they can
    /// reach <see cref="PetSyncResult{T}.Ok"/>: an empty one (a 2xx with nothing in it
    /// never says what was created), and a known plain-text rejection such as
    /// "Can not create duplicate client with ID 42", which the remote sends on a 2xx and
    /// which is not valid JSON — hence the text check rather than a parse.
    /// </summary>
    private static async Task<string?> ReadBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var raw = response.Content is null ? null : await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                $"Remote pet create returned HTTP {(int)response.StatusCode} with an empty body.");

        raw = raw.Trim();

        foreach (var rejection in RemoteTextRejections)
            if (raw.Contains(rejection, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Remote pet create rejected the pet (HTTP {(int)response.StatusCode}): {raw}");

        return raw;
    }

    /// <summary>
    /// Binds the success body to the caller's type. <c>string</c> is handed back verbatim;
    /// anything else is deserialized from the whole body. An unreadable body throws rather
    /// than quietly degrading to <c>default</c>. Goes through <see cref="ReadBodyAsync"/>,
    /// so the empty-body and plain-text rejection checks run before anything is parsed.
    /// </summary>
    private static async Task<T?> ReadTypedBodyAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var raw = await ReadBodyAsync(response, ct);
        if (raw is T text)
            return text;

        try
        {
            return JsonSerializer.Deserialize<T>(raw!, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Remote pet create body could not be read as {typeof(T).Name}: {raw}", ex);
        }
    }

    /// <summary>
    /// Reads the fault body from an error (4xx/5xx) response. Lenient by design — only the
    /// few guaranteed fields are bound, and a missing/non-JSON body yields null so the
    /// status code alone describes the failure. Read as text first: <c>ReadFromJsonAsync</c>
    /// rejects any media type that isn't <c>application/json</c> (or a <c>+json</c> suffix),
    /// and an error body is exactly where a remote is most likely to answer <c>text/html</c>.
    /// </summary>
    private static async Task<PetSyncFault?> TryReadFaultAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var raw = response.Content is null ? null : await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PetSyncFault>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ---- Wire contract ----
    // Request : { "pet": { "localId", "name", "breed", "price", "ageMonths", "categoryId", "status" } }
    // Response: the body is the remote id (taken verbatim); the generic overload binds
    //           the whole body to T.

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
}
