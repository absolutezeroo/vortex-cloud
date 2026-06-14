using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Marketplace;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Marketplace;

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
            return;

        await _grainFactory
            .GetMarketplacePurchaseGrain(ctx.PlayerId)
            .RedeemCreditsAsync(ct)
            .ConfigureAwait(false);
    }
}
