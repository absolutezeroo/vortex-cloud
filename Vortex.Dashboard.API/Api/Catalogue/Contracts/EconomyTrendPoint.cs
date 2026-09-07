namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>One bucket of one currency's line. Empty buckets are present with zeroes.</summary>
public sealed record EconomyTrendPoint(
    string Bucket,
    string Label,
    long Spend,
    long Earned,
    long Net,
    int TransactionCount
);
