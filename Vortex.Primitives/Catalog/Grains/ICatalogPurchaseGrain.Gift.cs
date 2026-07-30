using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Catalog.Grains;

public partial interface ICatalogPurchaseGrain
{
    /// <summary>
    /// Buys an offer on behalf of the grain's player and grants it to <paramref name="receiverId"/>.
    /// Identical to <c>PurchaseOfferFromCatalogAsync</c> in every respect that protects the economy
    /// -- club gate, HC discount, refund-on-failure, credit-spend tracking, audit events -- because
    /// the only thing a gift changes is whose inventory the furniture lands in.
    /// </summary>
    /// <exception cref="System.Exception">
    /// Throws <c>CatalogPurchaseException</c> with <see cref="CatalogPurchaseErrorType.OfferNotFound"/>
    /// for an unknown offer, <see cref="CatalogPurchaseErrorType.PurchaseFailed"/> when the offer is
    /// not giftable, <see cref="CatalogPurchaseErrorType.RequiresHabboClub"/> when the buyer lacks the
    /// club level, or a balance error when they cannot pay.
    /// </exception>
    public Task<CatalogOfferSnapshot> PurchaseOfferAsGiftAsync(
        CatalogType catalogType,
        int offerId,
        string extraParam,
        PlayerId receiverId,
        CancellationToken ct
    );
}
