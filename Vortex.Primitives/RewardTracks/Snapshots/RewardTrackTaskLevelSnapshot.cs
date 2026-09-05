using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>One stage of a task: how far the player must get, and what reaching it pays.</summary>
[GenerateSerializer, Immutable]
public sealed record RewardTrackTaskLevelSnapshot
{
    /// <summary>Zero-based stage index. Stages are ordered by <see cref="RequiredCount"/>.</summary>
    [Id(0)]
    public required int LevelIndex { get; init; }

    [Id(1)]
    public required int RequiredCount { get; init; }

    /// <summary>Track points awarded once, when the stage is first reached.</summary>
    [Id(2)]
    public required int PointsReward { get; init; }

    /// <summary>Only reachable with premium on this track.</summary>
    [Id(3)]
    public required bool Premium { get; init; }
}
