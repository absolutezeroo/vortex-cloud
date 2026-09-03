using Orleans;

namespace Vortex.Primitives.Sound.Snapshots;

/// <summary>
/// Where a room's jukebox is in its playlist, right now.
/// </summary>
/// <remarks>
/// <para>
/// The two "position" fields are positions in the <em>playlist</em>, not in the track — the client
/// exposes the first as <c>playPosition</c>. Time is carried by <see cref="SyncCountMs" /> alone:
/// the client divides it by 1000 and starts the song that many seconds in, which is what lets
/// someone walking into a room hear the same moment everyone else is hearing.
/// </para>
/// <para>
/// <see cref="Silent" /> is every field at -1, which is how the client is told to stop: it reads
/// <c>currentSongId != -1</c> as "playing".
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record NowPlayingSnapshot
{
    [Id(0)]
    public required int CurrentSongId { get; init; }

    [Id(1)]
    public required int CurrentIndex { get; init; }

    [Id(2)]
    public required int NextSongId { get; init; }

    [Id(3)]
    public required int NextIndex { get; init; }

    [Id(4)]
    public required int SyncCountMs { get; init; }

    public static readonly NowPlayingSnapshot Silent = new()
    {
        CurrentSongId = -1,
        CurrentIndex = -1,
        NextSongId = -1,
        NextIndex = -1,
        SyncCountMs = -1,
    };
}
