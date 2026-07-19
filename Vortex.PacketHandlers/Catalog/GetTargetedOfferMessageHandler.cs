using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Catalog;

public class GetTargetedOfferMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetTargetedOfferMessage>
{
    public async ValueTask HandleAsync(
        GetTargetedOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        TargetedOfferSnapshot? offer = await grainFactory
            .GetPlayerTargetedOfferGrain(ctx.PlayerId)
            .GetCurrentOfferAsync(ct)
            .ConfigureAwait(false);

        await TargetedOfferResponse
            .SendAsync(grainFactory, ctx.PlayerId, offer)
            .ConfigureAwait(false);
    }
}
