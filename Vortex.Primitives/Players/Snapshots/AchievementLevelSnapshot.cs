using Orleans;

namespace Vortex.Primitives.Players.Snapshots;

/// <summary>One level of an achievement definition. Thresholds are cumulative.</summary>
[GenerateSerializer, Immutable]
public sealed record AchievementLevelSnapshot
{
    [Id(0)]
    public required int Level { get; init; }

    [Id(1)]
    public required string BadgeCode { get; init; }

    /// <summary>Cumulative progress required to complete this level.</summary>
    [Id(2)]
    public required int ProgressRequirement { get; init; }

    [Id(3)]
    public required int RewardAmount { get; init; }

    [Id(4)]
    public required int RewardType { get; init; }

    /// <summary>Achievement-score points awarded when this level is completed.</summary>
    [Id(5)]
    public required int ScorePoints { get; init; }
}
