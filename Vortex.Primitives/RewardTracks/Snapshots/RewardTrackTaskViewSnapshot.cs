using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>A task resolved for one player.</summary>
[GenerateSerializer, Immutable]
public sealed record RewardTrackTaskViewSnapshot
{
    [Id(0)]
    public required string TaskId { get; init; }

    [Id(1)]
    public required string ActionCode { get; init; }

    [Id(2)]
    public required string Parameter { get; init; }

    [Id(3)]
    public required int ProgressCount { get; init; }

    [Id(4)]
    public required bool Premium { get; init; }

    [Id(5)]
    public required ImmutableArray<RewardTrackTaskLevelSnapshot> Levels { get; init; }
}
