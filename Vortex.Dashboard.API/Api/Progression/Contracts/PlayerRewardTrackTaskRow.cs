namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One task's progress.
/// </summary>
/// <param name="HighestPaidLevelIndex">The last rung already paid for, so re-running the level
/// walk cannot pay twice.</param>
public sealed record PlayerRewardTrackTaskRow(
    string TaskId,
    int ProgressCount,
    int HighestPaidLevelIndex
);
