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
/// Answers a client that has just found a jukebox: what is playing, and how far in.
/// </summary>
/// <remarks>
/// Sent when the client's room-object logic reports a jukebox, which is on entering a room and on
/// the furniture being placed. The offset in the answer is what lets someone arriving mid-song hear
/// the same moment as everyone already there.
/// </remarks>
public class GetNowPlayingMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetNowPlayingMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetNowPlayingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.RoomId <= 0)
        {
            return;
        }

        NowPlayingSnapshot playing = await _grainFactory
            .GetRoomJukebox(ctx.RoomId)
            .GetNowPlayingAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new NowPlayingMessageComposer
                {
                    CurrentSongId = playing.CurrentSongId,
                    CurrentIndex = playing.CurrentIndex,
                    NextSongId = playing.NextSongId,
                    NextIndex = playing.NextIndex,
                    SyncCountMs = playing.SyncCountMs,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
