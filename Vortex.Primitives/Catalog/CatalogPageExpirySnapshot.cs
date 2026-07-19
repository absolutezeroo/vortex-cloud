namespace Vortex.Primitives.Catalog;

/// <summary>Empty PageName hides the "expiring soon" landing-page promo widget (client sentinel).</summary>
public readonly record struct CatalogPageExpirySnapshot(
    string PageName,
    int SecondsToExpiry,
    string Image
);
