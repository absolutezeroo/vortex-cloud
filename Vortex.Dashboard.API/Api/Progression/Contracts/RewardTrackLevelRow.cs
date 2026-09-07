namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One rung of a task: how many, for how many points.</summary>
public sealed record RewardTrackLevelRow(
    int LevelIndex,
    int RequiredCount,
    int PointsReward,
    bool Premium
);
