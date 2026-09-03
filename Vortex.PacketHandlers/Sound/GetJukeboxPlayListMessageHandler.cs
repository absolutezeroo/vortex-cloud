using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Sound.Snapshots;
using Vortex.Protocol.Messages.Incoming.Sound;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.PacketHandlers.Sound;

/// <summary>
/// Sends the asking client the playlist of the jukebox standing in their room.
/// </summary>
/// <remarks>
/// The request names nothing — not the jukebox, not the room — so the room resolves its own, and an
/// empty answer covers both "no jukebox here" and "nothing loaded". The client draws the editor
/// either way.
/// </remarks>
public class GetJukeboxPlayListMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetJukeboxPlayListMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetJukeboxPlayListMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.RoomId <= 0)
        {
            return;
        }

        JukeboxPlaylistSnapshot playlist = await _grainFactory
            .GetRoomJukebox(ctx.RoomId)
            .GetPlaylistAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new JukeboxSongDisksMessageComposer
                {
                    Disks = playlist.Disks,
                    Capacity = playlist.Capacity,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
