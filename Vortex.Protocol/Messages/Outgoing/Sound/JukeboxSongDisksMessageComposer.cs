using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Sound.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.Sound;

/// <summary>
/// What the room's jukebox is loaded with. The client calls it the playlist.
/// </summary>
/// <remarks>
/// <see cref="Capacity" /> goes out first and is how many slots the editor draws, so a jukebox with
/// three disks and room for twenty is drawn as three disks and seventeen gaps.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record JukeboxSongDisksMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<SongDiskSnapshot> Disks { get; init; }

    [Id(1)]
    public required int Capacity { get; init; }
}
