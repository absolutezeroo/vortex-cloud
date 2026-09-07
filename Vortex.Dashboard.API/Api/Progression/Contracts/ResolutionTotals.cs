namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The statues as a whole.
/// </summary>
/// <param name="Players">Distinct players, not rows: one player can own several statues.</param>
public sealed record ResolutionTotals(
    int Offers,
    int EnabledOffers,
    int OrphanedOffers,
    int Taken,
    int Completed,
    int Live,
    int Expired,
    double CompletionRate,
    int Players
);
