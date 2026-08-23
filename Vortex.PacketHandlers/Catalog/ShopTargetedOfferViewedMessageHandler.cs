using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Catalog;

namespace Vortex.PacketHandlers.Catalog;

public class ShopTargetedOfferViewedMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ShopTargetedOfferViewedMessage>
{
    public async ValueTask HandleAsync(
        ShopTargetedOfferViewedMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.TargetedOfferId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerTargetedOfferGrain(ctx.PlayerId)
            .SetTrackingStateAsync(message.TargetedOfferId, message.TrackingState, ct)
            .ConfigureAwait(false);
    }
}
