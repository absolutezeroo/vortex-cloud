namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One offer in the sales leaderboard.
/// </summary>
/// <param name="OfferName">The offer's localisation id, falling back to its number for an offer
/// that has since been deleted -- the sale still happened and still counts.</param>
/// <param name="FurniIconUrl">The first bundled furniture's icon, which is the only picture an
/// offer has. Null when the offer bundles no furniture, or when it is gone.</param>
/// <param name="CatalogType">Empty for a sale recorded after the audit columns existed: the type
/// only ever lived in the JSON payload, and the rows that carry columns do not carry it.</param>
public sealed record CatalogOfferSales(
    int OfferId,
    string OfferName,
    string? FurniIconUrl,
    string CatalogType,
    int PurchaseCount,
    long Quantity,
    long CreditsSpent
);
