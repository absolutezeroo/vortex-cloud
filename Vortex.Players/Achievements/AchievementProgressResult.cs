using System.Collections.Immutable;

namespace Vortex.Players.Achievements;

/// <summary>
/// One level completed while applying progress. <see cref="RemovedBadgeCode"/> is the immediately
/// preceding level's badge (empty for level 1).
/// </summary>
public sealed record AchievementLevelUp
{
    public required int Level { get; init; }
    public required string BadgeCode { get; init; }
    public required string RemovedBadgeCode { get; init; }
    public required int RewardAmount { get; init; }
    public required int RewardType { get; init; }
    public required int ScorePoints { get; init; }
}

/// <summary>
/// Outcome of applying progress to one achievement: the new stored state and any levels crossed.
/// Pure data — the grain turns this into DB writes, badge/wallet grants and outbound composers.
/// </summary>
public sealed record AchievementProgressResult
{
    public required int NewProgress { get; init; }
    public required int NewCompletedLevels { get; init; }
    public required bool ProgressChanged { get; init; }
    public required ImmutableArray<AchievementLevelUp> LevelUps { get; init; }

    public bool LeveledUp => !LevelUps.IsEmpty;
}
