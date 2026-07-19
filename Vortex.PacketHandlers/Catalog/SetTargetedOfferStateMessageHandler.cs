using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Catalog;

public class SetTargetedOfferStateMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetTargetedOfferStateMessage>
{
    public async ValueTask HandleAsync(
        SetTargetedOfferStateMessage message,
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
