using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Sound;

/// <summary>
/// Where the room's playlist is, right now.
/// </summary>
/// <remarks>
/// <para>
/// The client reads <c>currentSongId == -1</c> as "stop" and anything else as "start this song
/// <c>syncCount / 1000</c> seconds in", which is the whole of the synchronisation: everyone in the
/// room is told the same offset and lands on the same bar.
/// </para>
/// <para>
/// This has to be pushed at every song boundary. The client's jukebox controller does not advance a
/// playlist on its own — its "song finished" handler is empty — so a server that only answers
/// <c>GetNowPlaying</c> plays one song and then goes quiet.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record NowPlayingMessageComposer : IComposer
{
    [Id(0)]
    public required int CurrentSongId { get; init; }

    /// <summary>Position in the playlist, not in the track: the client exposes it as <c>playPosition</c>.</summary>
    [Id(1)]
    public required int CurrentIndex { get; init; }

    /// <summary>What comes next, so the client can fetch its song info before it is needed.</summary>
    [Id(2)]
    public required int NextSongId { get; init; }

    [Id(3)]
    public required int NextIndex { get; init; }

    /// <summary>How far into the current song the room already is, in milliseconds.</summary>
    [Id(4)]
    public required int SyncCountMs { get; init; }
}
