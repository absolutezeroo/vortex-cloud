using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Marketplace.Snapshots;

namespace Turbo.Primitives.Marketplace.Grains;

public interface IMarketplaceSearchGrain : IGrainWithStringKey
{
    Task<(List<MarketplaceOfferSnapshot> Offers, int TotalFound)> GetOffersAsync(
        int minPrice,
        int maxPrice,
        string searchQuery,
        int sortOrder,
        CancellationToken ct
    );

    Task<MarketplaceItemStatsSnapshot> GetItemStatsAsync(int spriteId, CancellationToken ct);
}
