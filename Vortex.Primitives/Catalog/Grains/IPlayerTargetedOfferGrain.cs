using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Catalog.Snapshots;

namespace Vortex.Primitives.Catalog.Grains;

/// <summary>
/// Per-player grain for targeted offers: resolves which offer the player currently sees, handles the
/// special-price purchase (with per-player limit + expiry), and records the client's tracking state.
/// </summary>
public interface IPlayerTargetedOfferGrain : IGrainWithIntegerKey
{
    /// <summary>The first purchasable offer for this player, or null if none.</summary>
    public Task<TargetedOfferSnapshot?> GetCurrentOfferAsync(CancellationToken ct);

    /// <summary>The next purchasable offer after <paramref name="afterOfferId"/>, or null if none.</summary>
    public Task<TargetedOfferSnapshot?> GetNextOfferAsync(int afterOfferId, CancellationToken ct);

    /// <summary>
    /// Purchases the offer (debits credits/activity points, grants the bundle). Returns the offer's
    /// refreshed snapshot on success or null if it could not be purchased (unknown/expired/over limit).
    /// </summary>
    public Task<TargetedOfferSnapshot?> PurchaseAsync(
        int offerId,
        int quantity,
        CancellationToken ct
    );

    /// <summary>Records the client-reported tracking state (viewed/minimised/closed/...).</summary>
    public Task SetTrackingStateAsync(int offerId, int trackingState, CancellationToken ct);
}
