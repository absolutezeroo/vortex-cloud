namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One thing in an offer's bundle.
/// </summary>
/// <param name="FurnitureName">Null when the product is not furniture, or when the definition is
/// gone. The icon follows it.</param>
public sealed record TargetedOfferProduct(
    int Id,
    string ProductCode,
    int? FurnitureDefinitionEntityId,
    string? FurnitureName,
    string? FurnitureIconUrl,
    int Quantity
);
