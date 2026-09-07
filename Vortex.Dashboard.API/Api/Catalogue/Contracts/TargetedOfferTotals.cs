namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>The window's sales, in both currencies an offer can be priced in.</summary>
public sealed record TargetedOfferTotals(
    int PurchaseCount,
    long TotalCreditsSpent,
    long TotalActivityPointsSpent,
    long TotalQuantity
);
