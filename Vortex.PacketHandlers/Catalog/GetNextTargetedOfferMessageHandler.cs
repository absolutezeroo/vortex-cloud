using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Catalog;

public class GetNextTargetedOfferMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetNextTargetedOfferMessage>
{
    public async ValueTask HandleAsync(
        GetNextTargetedOfferMessage message,
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
            .GetNextOfferAsync(message.OfferId, ct)
            .ConfigureAwait(false);

        await TargetedOfferResponse
            .SendAsync(grainFactory, ctx.PlayerId, offer)
            .ConfigureAwait(false);
    }
}
