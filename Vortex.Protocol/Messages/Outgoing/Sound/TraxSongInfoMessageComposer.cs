using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Sound.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.Sound;

/// <summary>
/// The answer to <c>GetSongInfo</c>: everything the client needs to name and play a song.
/// </summary>
/// <remarks>
/// One entry per song, and the client tolerates a short list — a song it asked about and did not get
/// back stays unknown and is simply never played. So an id this hotel does not ship is left out
/// rather than answered with a placeholder.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record TraxSongInfoMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<SongSnapshot> Songs { get; init; }
}
