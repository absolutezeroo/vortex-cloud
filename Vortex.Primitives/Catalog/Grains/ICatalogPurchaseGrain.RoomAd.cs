using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Catalog.Snapshots;

namespace Vortex.Primitives.Catalog.Grains;

public partial interface ICatalogPurchaseGrain
{
    /// <summary>Debits the offer's cost and creates a RoomAdvertisementEntity for the room, atomic
    /// via the same ExecutePurchaseAsync pattern as PurchaseOfferFromCatalogAsync -- there's no
    /// inventory grant here, room ads just advertise an already-owned room. Returns the purchased
    /// offer so the caller can reuse the same PurchaseOKMessageComposer the normal catalog purchase
    /// path sends.</summary>
    public Task<CatalogOfferSnapshot> PurchaseRoomAdAsync(
        int offerId,
        int roomId,
        string name,
        string? description,
        bool extended,
        int categoryId,
        CancellationToken ct
    );
}
