using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Sound.Providers;
using Vortex.Primitives.Sound.Snapshots;
using Vortex.Protocol.Messages.Incoming.Sound;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.PacketHandlers.Sound;

/// <summary>
/// Turns the official code a song-disk catalogue offer carries into the numeric song id.
/// </summary>
/// <remarks>
/// Silence is the right answer for a code this hotel does not ship: the client only replaces the
/// page's song id when the code in the reply matches the one it sent, so there is nothing useful to
/// say and no error message it would show.
/// </remarks>
public class GetOfficialSongIdMessageHandler(ISongProvider songs)
    : IMessageHandler<GetOfficialSongIdMessage>
{
    private readonly ISongProvider _songs = songs;

    public async ValueTask HandleAsync(
        GetOfficialSongIdMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (_songs.TryGetSongByOfficialId(message.OfficialSongId) is not { } song)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new OfficialSongIdMessageComposer
                {
                    OfficialSongId = message.OfficialSongId,
                    SongId = song.Id,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
