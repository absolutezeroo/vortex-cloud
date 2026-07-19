using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
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

        // The client updates the offer view locally on purchase and expects no offer echo (re-sending
        // it would re-maximise the offer the client just minimised); the granted furniture's own
        // inventory composers are the purchase feedback.
        await grainFactory
            .GetPlayerTargetedOfferGrain(ctx.PlayerId)
            .PurchaseAsync(message.OfferId, message.Quantity, ct)
            .ConfigureAwait(false);
    }
}
