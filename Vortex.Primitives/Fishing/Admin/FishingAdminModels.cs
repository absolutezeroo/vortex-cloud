namespace Vortex.Primitives.Fishing.Admin;

/// <summary>
/// One fishing zone as an operator edits it: a spot furni class, the level it needs, and how much a
/// fresh spot holds before it runs dry.
/// </summary>
public sealed record FishingZoneSpec(
    string NameKey,
    string FurniClass,
    int RequiredLevel,
    int MinCatches,
    int MaxCatches
);

/// <summary>
/// One species. Every rate here is an operator-editable guess reconstructed from Origins, which is
/// the whole reason these live in a table.
/// </summary>
/// <remarks>
/// <paramref name="CatchRate" /> is tenths of a percent — 850 is 85% — and it is the entire
/// difficulty model, because an ordinary catch has no minigame for a player to be good at.
/// <paramref name="ActiveHours" /> is a 24-bit mask and <paramref name="ActiveWeekdays" /> a 7-bit
/// one starting at Sunday.
/// </remarks>
public sealed record FishingSpeciesSpec(
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
    int ActiveSeasons
);

/// <summary>
/// One rod tier. Not the fishing level: the rod raises multipliers and the Hook Havoc chance, the
/// level unlocks zones, and fusing them was the original design's worst mistake.
/// </summary>
public sealed record FishingRodTierSpec(
    int Quality,
    int XpThreshold,
    string NameKey,
    int HandItemId,
    int CatchMultiplier,
    int GoldenMultiplier,
    int HookHavocChance
);

/// <summary>One step of the fishing level curve.</summary>
public sealed record FishingLevelSpec(int Level, int XpThreshold);

/// <summary>The outcome of one fishing admin write.</summary>
public sealed record FishingAdminResult
{
    public required bool Success { get; init; }

    public required string Error { get; init; }

    /// <summary>The row written, when there is one.</summary>
    public int RowId { get; init; }

    /// <summary>How many species the live cache holds after the reload this write triggered.</summary>
    public int SpeciesLoaded { get; init; }

    public static FishingAdminResult Ok(int rowId, int speciesLoaded = 0) =>
        new()
        {
            Success = true,
            Error = string.Empty,
            RowId = rowId,
            SpeciesLoaded = speciesLoaded,
        };

    public static FishingAdminResult Fail(string error) => new() { Success = false, Error = error };
}
