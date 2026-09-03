using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Sound.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.Sound;

/// <summary>
/// The sound machine's playlist — the Trax composer's own list, not the jukebox's.
/// </summary>
/// <remarks>
/// Carries the songs themselves rather than the disks holding them, which is the difference from
/// <see cref="JukeboxSongDisksMessageComposer" />: the composer names what it can play, the jukebox
/// names what is loaded into it.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record PlayListMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<SongSnapshot> Songs { get; init; }

    /// <summary>How far into the list playback stands, in milliseconds.</summary>
    [Id(1)]
    public required int SynchronizationCountMs { get; init; }
}
