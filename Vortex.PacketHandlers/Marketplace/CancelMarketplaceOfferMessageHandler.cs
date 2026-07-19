using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Marketplace;
using Vortex.Primitives.Messages.Outgoing.Marketplace;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Marketplace;

public class CancelMarketplaceOfferMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<CancelMarketplaceOfferMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        CancelMarketplaceOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        bool success = await _grainFactory
            .GetMarketplacePurchaseGrain(ctx.PlayerId)
            .CancelOrRedeemOfferAsync(message.OfferId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new MarketplaceCancelOfferResultEventMessageComposer
                {
                    OfferId = message.OfferId,
                    Success = success,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
