using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>One player's progress on one task.</summary>
[GenerateSerializer, Immutable]
public sealed record PlayerTaskProgressSnapshot
{
    [Id(0)]
    public required string TaskId { get; init; }

    [Id(1)]
    public required int ProgressCount { get; init; }

    /// <summary>
    /// Index of the highest stage already paid for, or -1 when none is. What stops a reconnect,
    /// a retry or a second identical event from paying the same stage twice: a stage pays when it
    /// moves this number up, and only then.
    /// </summary>
    [Id(2)]
    public required int HighestPaidLevelIndex { get; init; }
}
