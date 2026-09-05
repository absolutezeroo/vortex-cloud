using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>
/// One task advancing, as the incremental push carries it: the client patches these four fields in
/// place rather than redrawing the track.
/// </summary>
[GenerateSerializer, Immutable]
public readonly record struct RewardTrackProgressNotice
{
    [Id(0)]
    public required string TrackId { get; init; }

    [Id(1)]
    public required string TaskId { get; init; }

    [Id(2)]
    public required int ProgressCount { get; init; }

    [Id(3)]
    public required int Points { get; init; }
}
