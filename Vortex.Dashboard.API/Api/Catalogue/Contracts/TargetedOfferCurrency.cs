namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>One currency an offer may be priced in.</summary>
public sealed record TargetedOfferCurrency(
    int Id,
    string? Name,
    string Type,
    int? ActivityPointType
);
