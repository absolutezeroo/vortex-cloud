using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Catalog;

public class PurchaseTargetedOfferMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<PurchaseTargetedOfferMessage>
{
    public async ValueTask HandleAsync(
        PurchaseTargetedOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.OfferId <= 0)
        {
            return;
        }

        TargetedOfferSnapshot? offer = await grainFactory
            .GetPlayerTargetedOfferGrain(ctx.PlayerId)
            .PurchaseAsync(message.OfferId, message.Quantity, ct)
            .ConfigureAwait(false);

        // Echo the offer's refreshed state (remaining purchases) so the client updates in place.
        await TargetedOfferResponse
            .SendAsync(grainFactory, ctx.PlayerId, offer)
            .ConfigureAwait(false);
    }
}
