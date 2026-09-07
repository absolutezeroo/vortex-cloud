namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// The window's trade, except the listing count which is right now.
/// </summary>
/// <param name="ActiveListings">Offers standing at this moment, not offers listed in the window.</param>
/// <param name="AveragePrice">Volume over sales, 0 when nothing sold.</param>
public sealed record MarketplaceTotals(
    int ActiveListings,
    int SoldCount,
    long TotalVolume,
    double AveragePrice
);
