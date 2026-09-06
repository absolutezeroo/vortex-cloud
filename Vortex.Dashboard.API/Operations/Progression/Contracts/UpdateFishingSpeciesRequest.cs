using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdateFishingSpeciesRequest(
    int SpeciesId,
    int ZoneId,
    string NameKey,
    int RequiredLevel,
    int RarityStars,
    int CatchRate,
    int RarityWeight,
    int MinWeight,
    int MaxWeight,
    int XpReward,
    int GoldenXpBonus,
    int CurrencyReward,
    int ActiveHours,
    int ActiveWeekdays,
    int ActiveSeasons,
    string Reason
) : IReasonedRequest;
