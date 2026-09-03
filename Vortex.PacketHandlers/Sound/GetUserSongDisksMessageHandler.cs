using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Sound;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.PacketHandlers.Sound;

/// <summary>
/// Hands back the song disks the player is holding, so the jukebox editor has something to show.
/// </summary>
public class GetUserSongDisksMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetUserSongDisksMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetUserSongDisksMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new UserSongDisksInventoryMessageComposer
                {
                    Disks = await _grainFactory
                        .GetInventoryGrain(ctx.PlayerId)
                        .GetSongDisksAsync(ct)
                        .ConfigureAwait(false),
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
