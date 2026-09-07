namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One rod quality.
/// </summary>
/// <param name="CatchMultiplier">Per mille: 1000 is unchanged. Same for the golden one.</param>
public sealed record FishingRodTierRow(
    int Id,
    int Quality,
    int XpThreshold,
    string NameKey,
    int HandItemId,
    int CatchMultiplier,
    int GoldenMultiplier,
    int HookHavocChance
);
