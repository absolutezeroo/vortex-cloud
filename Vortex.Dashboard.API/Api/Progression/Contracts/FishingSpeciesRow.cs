namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One catchable species.
/// </summary>
/// <param name="CatchRatePercent">The stored rate written as a percentage.</param>
/// <param name="DrawSharePercent">The share of the draw this species really has. It competes only
/// with the others in its own zone, so the denominator is per zone -- computed here because a page
/// working it out itself would be a second implementation of the rule the server draws by.</param>
/// <param name="ActiveHours">A 24-bit mask, one bit per hour; <paramref name="AllHours"/> is the
/// same fact for the common case.</param>
/// <param name="ActiveWeekdays">A 7-bit mask, likewise.</param>
/// <param name="ActiveSeasons">A 4-bit mask; there is no all-seasons shorthand because the page
/// draws the four individually.</param>
public sealed record FishingSpeciesRow(
    int Id,
    int ZoneId,
    string NameKey,
    int RequiredLevel,
    int RarityStars,
    int CatchRate,
    double CatchRatePercent,
    int RarityWeight,
    double DrawSharePercent,
    int MinWeight,
    int MaxWeight,
    int XpReward,
    int GoldenXpBonus,
    int CurrencyReward,
    int ActiveHours,
    int ActiveWeekdays,
    int ActiveSeasons,
    bool AllHours,
    bool AllWeekdays
);
