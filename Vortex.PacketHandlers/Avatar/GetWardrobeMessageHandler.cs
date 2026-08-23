using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Protocol.Messages.Incoming.Avatar;
using Vortex.Protocol.Messages.Outgoing.Avatar;

namespace Vortex.PacketHandlers.Avatar;

public class GetWardrobeMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetWardrobeMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetWardrobeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        List<PlayerWardrobeOutfitSnapshot> outfits = await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .GetWardrobeAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new WardrobeMessageComposer { Outfits = outfits }, ct)
            .ConfigureAwait(false);
    }
}
