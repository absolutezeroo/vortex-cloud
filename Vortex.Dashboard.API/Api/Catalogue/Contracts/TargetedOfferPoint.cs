namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>One bucket of the sales chart. Empty buckets are present with zeroes.</summary>
public sealed record TargetedOfferPoint(
    string Bucket,
    string Label,
    int PurchaseCount,
    long CreditsSpent
);
