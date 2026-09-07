namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One offer in the sales leaderboard.
/// </summary>
/// <param name="OfferName">Its title, falling back to its identifier and then to its number for an
/// offer that has since been deleted -- the sale still happened and still counts.</param>
public sealed record TargetedOfferSales(
    int OfferId,
    string OfferName,
    string? FurniIconUrl,
    int PurchaseCount,
    long Quantity,
    long CreditsSpent,
    long ActivityPointsSpent
);
