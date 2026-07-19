using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Snapshots;

namespace Vortex.Primitives.Catalog.Grains;

public partial interface ICatalogPurchaseGrain : IGrainWithIntegerKey
{
    public Task<CatalogOfferSnapshot> PurchaseOfferFromCatalogAsync(
        CatalogType catalogType,
        int offerId,
        string extraParam,
        int quantity,
        CancellationToken ct
    );
}
