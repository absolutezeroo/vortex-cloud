using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the fishing content tables, each carrying a mandatory audited <c>Reason</c>.
/// </summary>
/// <remarks>
/// The units are the table's, not the page's: <c>CatchRate</c> and <c>HookHavocChance</c> are tenths
/// of a percent, the multipliers are thousandths, and the two calendars are bit masks. The page
/// renders them as percentages, factors and checkboxes; nothing between here and the database
/// reinterprets them.
/// </remarks>
public sealed record CreateFishingZoneRequest(
    string NameKey,
    string FurniClass,
    int RequiredLevel,
    int MinCatches,
    int MaxCatches,
    string Reason
) : IReasonedRequest;

public sealed record UpdateFishingZoneRequest(
    int ZoneId,
    string NameKey,
    string FurniClass,
    int RequiredLevel,
    int MinCatches,
    int MaxCatches,
    string Reason
) : IReasonedRequest;

public sealed record DeleteFishingZoneRequest(int ZoneId, string Reason) : IReasonedRequest;

public sealed record CreateFishingSpeciesRequest(
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

public sealed record DeleteFishingSpeciesRequest(int SpeciesId, string Reason) : IReasonedRequest;

public sealed record CreateFishingRodTierRequest(
    int Quality,
    int XpThreshold,
    string NameKey,
    int HandItemId,
    int CatchMultiplier,
    int GoldenMultiplier,
    int HookHavocChance,
    string Reason
) : IReasonedRequest;

public sealed record UpdateFishingRodTierRequest(
    int TierId,
    int Quality,
    int XpThreshold,
    string NameKey,
    int HandItemId,
    int CatchMultiplier,
    int GoldenMultiplier,
    int HookHavocChance,
    string Reason
) : IReasonedRequest;

public sealed record DeleteFishingRodTierRequest(int TierId, string Reason) : IReasonedRequest;

public sealed record CreateFishingLevelRequest(int Level, int XpThreshold, string Reason)
    : IReasonedRequest;

public sealed record UpdateFishingLevelRequest(
    int LevelId,
    int Level,
    int XpThreshold,
    string Reason
) : IReasonedRequest;

public sealed record DeleteFishingLevelRequest(int LevelId, string Reason) : IReasonedRequest;

public sealed record ReloadFishingRequest(string Reason) : IReasonedRequest;
