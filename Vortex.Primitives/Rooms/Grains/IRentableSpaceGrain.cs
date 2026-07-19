using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Rooms.Snapshots;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>
/// Per-instance grain for one placed rentable-space furniture item (key = furniture id).
/// Owns rent / cancel / expiry transitions and all status queries.
/// </summary>
public interface IRentableSpaceGrain : IGrainWithIntegerKey
{
    /// <summary>
    /// Current status as seen by <paramref name="viewerPlayerId"/>. Never returns null;
    /// returns a "not rented / can rent" snapshot even when the space state row is absent.
    /// </summary>
    Task<RentableSpaceStatusSnapshot> GetStatusAsync(int viewerPlayerId, CancellationToken ct);

    /// <summary>
    /// Attempt to rent this space for <paramref name="renterPlayerId"/>.
    /// Debits the wallet and writes the ledger entry on success.
    /// Returns null on success; a failure reason on any validation or balance error.
    /// </summary>
    Task<int?> RentAsync(int renterPlayerId, CancellationToken ct);

    /// <summary>
    /// Cancel an active rental.
    /// <paramref name="isStaff"/> bypasses the ownership check (DATA-MODEL §3.4).
    /// Returns false when the space is not rented or the actor is not authorized.
    /// </summary>
    Task<bool> CancelRentAsync(int actorPlayerId, bool isStaff, CancellationToken ct);

    /// <summary>
    /// Called by the expiry timer. Clears the rental and returns tagged furniture to
    /// the renter's inventory (DATA-MODEL §3.3 expiry clause). No-op if not rented.
    /// </summary>
    Task ExpireAsync(CancellationToken ct);

    /// <summary>
    /// Returns the current owner-configurable terms for this space.
    /// Fields are zeroed when no terms row exists yet.
    /// </summary>
    Task<RentableSpaceConfigSnapshot> GetConfigAsync(CancellationToken ct);

    /// <summary>
    /// Creates or replaces the <c>rentable_space_terms</c> row for this furniture instance.
    /// Only the furniture owner (or staff when <paramref name="isStaff"/> is true) may call this.
    /// Returns false when the actor is not authorized or <paramref name="currencyTypeId"/> is unknown.
    /// </summary>
    Task<bool> ConfigureAsync(
        int actorPlayerId,
        bool isStaff,
        int price,
        int currencyTypeId,
        int rentDurationSeconds,
        bool requiresHc,
        CancellationToken ct
    );
}
