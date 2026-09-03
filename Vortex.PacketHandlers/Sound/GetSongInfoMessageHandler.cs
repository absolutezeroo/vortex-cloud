using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Sound.Providers;
using Vortex.Primitives.Sound.Snapshots;
using Vortex.Protocol.Messages.Incoming.Sound;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.PacketHandlers.Sound;

/// <summary>
/// Names the songs the client has ids for and nothing else.
/// </summary>
/// <remarks>
/// The client will not play a song it cannot name, so this answer is the gate in front of every
/// other part of the feature — a jukebox playlist, a disk in the hand, a song-disk catalogue page all
/// stall here. Unanswered, they stall forever: there is no retry, the client asks once per id.
/// <para>
/// Ids this hotel does not ship are left out rather than answered with a placeholder. A short list is
/// something the client already handles; a song with no data is one it would try to play.
/// </para>
/// </remarks>
public class GetSongInfoMessageHandler(ISongProvider songs) : IMessageHandler<GetSongInfoMessage>
{
    private readonly ISongProvider _songs = songs;

    public async ValueTask HandleAsync(
        GetSongInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (message.SongIds.IsDefaultOrEmpty)
        {
            return;
        }

        ImmutableArray<SongSnapshot>.Builder found = ImmutableArray.CreateBuilder<SongSnapshot>(
            message.SongIds.Length
        );

        foreach (int songId in message.SongIds)
        {
            if (_songs.TryGetSong(songId) is { } song)
            {
                found.Add(song);
            }
        }

        if (found.Count == 0)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new TraxSongInfoMessageComposer { Songs = found.ToImmutable() },
                ct
            )
            .ConfigureAwait(false);
    }
}
