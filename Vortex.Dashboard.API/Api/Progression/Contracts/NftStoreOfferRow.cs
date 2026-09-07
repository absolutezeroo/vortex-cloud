namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One shop offer.
/// </summary>
/// <param name="Resolved">Whether the furniture exists. The grain refuses a sale that would take
/// emeralds and hand over nothing; this is the only place that says so beforehand.</param>
/// <param name="IsNft">Whether the client will treat it as a collectible at all. It decides purely
/// from the classname -- GroupItem.isNft() is className.indexOf("nft_") == 0 -- so anything else is
/// hidden from the Collectibles category and listed as ordinary furniture, however it was bought.</param>
public sealed record NftStoreOfferRow(
    int Id,
    string ProductCode,
    int EmeraldPrice,
    bool IsFeatured,
    bool IsLimited,
    int MintLimit,
    int SoldCount,
    string ItemTypeId,
    int ProductTypeId,
    int Score,
    string Rarity,
    bool Enabled,
    int SortOrder,
    bool Resolved,
    bool SoldOut,
    bool IsNft,
    string? IconUrl
);
