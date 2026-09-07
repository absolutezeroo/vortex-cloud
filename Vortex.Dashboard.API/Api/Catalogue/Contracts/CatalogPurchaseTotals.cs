namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// The window, not the whole catalogue.
/// </summary>
/// <param name="TotalQuantity">Items handed over, which is higher than the purchase count wherever
/// an offer sells a bundle.</param>
public sealed record CatalogPurchaseTotals(
    int PurchaseCount,
    long TotalCreditsSpent,
    long TotalQuantity
);
