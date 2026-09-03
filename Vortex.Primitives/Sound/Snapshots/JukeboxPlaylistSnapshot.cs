using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Sound.Snapshots;

/// <summary>
/// What a room's jukebox is loaded with, in play order.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record JukeboxPlaylistSnapshot
{
    [Id(0)]
    public required ImmutableArray<SongDiskSnapshot> Disks { get; init; }

    /// <summary>How many disks the jukebox accepts. The client draws the empty slots from it.</summary>
    [Id(1)]
    public required int Capacity { get; init; }

    public static readonly JukeboxPlaylistSnapshot Empty = new() { Disks = [], Capacity = 0 };
}
