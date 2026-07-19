using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Marketplace.Snapshots;

namespace Vortex.Primitives.Marketplace.Grains;

public interface IMarketplacePurchaseGrain : IGrainWithIntegerKey
{
    Task<(int Result, int OfferId)> MakeOfferAsync(
        int furnitureItemId,
        int price,
        CancellationToken ct
    );
    Task<bool> CancelOrRedeemOfferAsync(int offerId, CancellationToken ct);
    Task<int> BuyOfferAsync(int offerId, CancellationToken ct);
    Task<int> RedeemCreditsAsync(CancellationToken ct);
    Task<(int CreditsOwed, List<MarketplaceOfferSnapshot> Offers)> GetOwnOffersAsync(
        CancellationToken ct
    );
}
