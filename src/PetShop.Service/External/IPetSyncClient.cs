using System.Text.Json.Serialization;
using PetShop.Domain.Entities;

namespace PetShop.Service.External;

/// <summary>
/// Replicates a pet to the external pet service. Implementations are best-effort:
/// they never throw for transport/HTTP errors (those are reported on
/// <see cref="PetSyncResult"/>), so a remote failure cannot fail the local create.
/// Genuine caller cancellation is still propagated.
/// </summary>
public interface IPetSyncClient
{
    /// <summary>
    /// Syncs the pet and returns only the remote id (read from the response's
    /// <c>data.id</c>). Back-compat overload — equivalent to the generic form
    /// with the remote id projected onto <see cref="PetSyncResult.RemoteId"/>.
    /// </summary>
    Task<PetSyncResult> CreateAsync(Pet pet, CancellationToken ct = default);

    /// <summary>
    /// Syncs the pet and deserializes the remote response's <c>data</c> property
    /// into a caller-supplied type.
    /// </summary>
    /// <typeparam name="T">The shape to bind the remote <c>data</c> object to.</typeparam>
    Task<PetSyncResult<T>> CreateAsync<T>(Pet pet, CancellationToken ct = default);
}

/// <summary>Outcome of a remote sync attempt.</summary>
/// <param name="Success">True when the remote service accepted the pet.</param>
/// <param name="Skipped">True when syncing is disabled, so no call was made.</param>
/// <param name="RemoteId">Identifier returned by the remote service, if any.</param>
/// <param name="Error">Failure reason when <paramref name="Success"/> is false.</param>
public record PetSyncResult(bool Success, bool Skipped, string? RemoteId, string? Error)
{
    public static PetSyncResult Disabled() => new(false, true, null, null);
    public static PetSyncResult Ok(string? remoteId) => new(true, false, remoteId, null);
    public static PetSyncResult Failed(string error) => new(false, false, null, error);
}

/// <summary>
/// Remote fault body returned on an error status (4xx/5xx). Only the fields the
/// remote guarantees are modelled; anything else in the body is ignored, and any
/// guaranteed field absent from a malformed body deserializes to <c>null</c>.
/// </summary>
public record PetSyncFault(
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("message")] string? Message);

/// <summary>Outcome of a remote sync attempt carrying a caller-typed payload.</summary>
/// <typeparam name="T">Type the remote <c>data</c> object is deserialized into.</typeparam>
/// <param name="Success">True when the remote service accepted the pet (2xx).</param>
/// <param name="Skipped">True when syncing is disabled, so no call was made.</param>
/// <param name="Data">Remote 2xx payload, deserialized into <typeparamref name="T"/>, if any.</param>
/// <param name="Fault">Remote fault body, parsed from a 4xx/5xx response, if any.</param>
/// <param name="Error">Human-readable failure reason when <paramref name="Success"/> is false.</param>
public record PetSyncResult<T>(bool Success, bool Skipped, T? Data, PetSyncFault? Fault, string? Error)
{
    public static PetSyncResult<T> Disabled() => new(false, true, default, null, null);
    public static PetSyncResult<T> Ok(T? data) => new(true, false, data, null, null);
    public static PetSyncResult<T> Failed(string error, PetSyncFault? fault = null)
        => new(false, false, default, fault, error);
}
