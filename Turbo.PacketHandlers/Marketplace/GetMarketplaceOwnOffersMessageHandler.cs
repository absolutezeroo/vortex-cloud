using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Marketplace.Snapshots;
using Turbo.Primitives.Messages.Incoming.Marketplace;
using Turbo.Primitives.Messages.Outgoing.Marketplace;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Marketplace;

public class GetMarketplaceOwnOffersMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetMarketplaceOwnOffersMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetMarketplaceOwnOffersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        (int creditsOwed, List<MarketplaceOfferSnapshot> offers) = await _grainFactory
            .GetMarketplacePurchaseGrain(ctx.PlayerId)
            .GetOwnOffersAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new MarketPlaceOwnOffersEventMessageComposer
                {
                    CreditsWaiting = creditsOwed,
                    Offers = offers,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
