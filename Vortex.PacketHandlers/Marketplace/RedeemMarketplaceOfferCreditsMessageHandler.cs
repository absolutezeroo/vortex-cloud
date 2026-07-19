using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Marketplace;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Marketplace;

public class RedeemMarketplaceOfferCreditsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RedeemMarketplaceOfferCreditsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        RedeemMarketplaceOfferCreditsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetMarketplacePurchaseGrain(ctx.PlayerId)
            .RedeemCreditsAsync(ct)
            .ConfigureAwait(false);
    }
}
