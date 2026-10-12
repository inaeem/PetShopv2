using System.Text.Json.Serialization;
using PetShop.Domain.Entities;

namespace PetShop.Service.External;

/// <summary>
/// Replicates a pet to the external pet service. Transport errors (connect failures,
/// timeouts) are reported on <see cref="PetSyncResult"/> rather than thrown; a remote
/// that answers unusably — a non-success status, or a success with an empty/unreadable
/// body — throws <see cref="InvalidOperationException"/>. Caller cancellation is
/// propagated. Callers for whom a remote failure must not fail local work catch around
/// the call.
/// </summary>
public interface IPetSyncClient
{
    /// <summary>
    /// Syncs the pet and returns the remote id — the response body, taken verbatim.
    /// Back-compat overload — equivalent to the generic form with the body projected
    /// onto <see cref="PetSyncResult.RemoteId"/>.
    /// </summary>
    Task<PetSyncResult> CreateAsync(Pet pet, CancellationToken ct = default);

    /// <summary>
    /// Syncs the pet and binds the remote response body to a caller-supplied type.
    /// <c>string</c> yields the body verbatim; any other type is deserialized from it.
    /// </summary>
    /// <typeparam name="T">The shape to bind the remote response body to.</typeparam>
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
