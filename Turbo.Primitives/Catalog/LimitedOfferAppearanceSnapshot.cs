namespace Turbo.Primitives.Catalog;

/// <summary>PageId/OfferId &gt;= 0 means a limited edition item is available right now (client shows
/// a "get it" button); otherwise AppearsInSeconds &gt; 0 means a countdown to the next one; both
/// zero/negative means hide the promo entirely -- matches the WIN63 client's own
/// NextLimitedRareCountdownWidget interpretation of these fields.</summary>
public readonly record struct LimitedOfferAppearanceSnapshot(
    int AppearsInSeconds,
    int PageId,
    int OfferId,
    string ProductType
);
