namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>One bucket of the lifecycle chart.</summary>
public sealed record ClubLifecyclePoint(
    string Bucket,
    string Label,
    int Purchases,
    int Renewals,
    int Expired
);
