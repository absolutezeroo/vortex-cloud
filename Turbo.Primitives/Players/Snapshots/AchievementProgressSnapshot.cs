using Orleans;

namespace Turbo.Primitives.Players.Snapshots;

/// <summary>
/// Wire-ready view of a player's standing on one achievement, mapping 1:1 to the client's
/// AchievementData payload (see the WIN63 20260701 client). All progress figures are cumulative.
/// <see cref="Level"/> is the level the player is currently working toward (completed levels + 1),
/// except when <see cref="FinalLevel"/> is true, where it equals <see cref="LevelCount"/>.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record AchievementProgressSnapshot
{
    [Id(0)]
    public required int AchievementId { get; init; }

    [Id(1)]
    public required int Level { get; init; }

    /// <summary>Badge of the highest completed level, or empty when no level is completed yet.</summary>
    [Id(2)]
    public required string BadgeCode { get; init; }

    /// <summary>Cumulative progress at which the current level began (0 for level 1).</summary>
    [Id(3)]
    public required int ScoreAtStartOfLevel { get; init; }

    /// <summary>Cumulative progress at which the current level completes.</summary>
    [Id(4)]
    public required int LevelMaxScore { get; init; }

    [Id(5)]
    public required int LevelRewardAmount { get; init; }

    [Id(6)]
    public required int LevelRewardType { get; init; }

    /// <summary>Cumulative current progress.</summary>
    [Id(7)]
    public required int CurrentProgress { get; init; }

    [Id(8)]
    public required bool FinalLevel { get; init; }

    [Id(9)]
    public required string Category { get; init; }

    [Id(10)]
    public required string SubCategory { get; init; }

    [Id(11)]
    public required int LevelCount { get; init; }

    [Id(12)]
    public required int DisplayMethod { get; init; }

    [Id(13)]
    public required int State { get; init; }
}
