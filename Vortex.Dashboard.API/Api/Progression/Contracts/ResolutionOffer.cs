namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One achievement a statue offers, with how its challenges went.
/// </summary>
/// <param name="Orphaned">The offer points at an achievement that no longer exists. The table
/// cannot say this on its own, and the grain silently drops such rows -- so the offer is configured,
/// enabled, and never reaches a player.</param>
/// <param name="TargetLevelOffset">How far past the player's current level the challenge asks them
/// to get.</param>
/// <param name="CompletionRate">Completed over taken, as a percentage; 0 when nobody took it.</param>
public sealed record ResolutionOffer(
    int Id,
    int AchievementId,
    string? AchievementName,
    string? Category,
    bool Orphaned,
    int LevelCount,
    int TargetLevelOffset,
    int SortOrder,
    bool Enabled,
    int Taken,
    int Completed,
    int Live,
    int Expired,
    double CompletionRate
);
