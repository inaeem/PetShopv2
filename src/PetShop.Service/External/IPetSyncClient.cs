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
    Task<PetSyncResult> CreateAsync(Pet pet, CancellationToken ct = default);
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
