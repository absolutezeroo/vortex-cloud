namespace Vortex.Database.Entities.Marketplace;

public enum MarketplaceOfferState
{
    Active = 1,
    Sold = 2,
    Cancelled = 3,
    Expired = 4,

    /// <summary>
    /// The offer row exists, the item has not left the seller's inventory yet, and no buyer can see
    /// it. Listing used to remove the item first and insert the offer second, so a failure in
    /// between destroyed the furniture — gone from the inventory and held by nothing. Writing the
    /// offer first means the worst case is an offer nobody can buy, which is recoverable; the worst
    /// case the other way round was not.
    /// </summary>
    PendingRemoval = 5,
}
