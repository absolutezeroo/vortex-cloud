using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Marketplace.Snapshots;
using Vortex.Primitives.Messages.Incoming.Marketplace;
using Vortex.Primitives.Messages.Outgoing.Marketplace;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Marketplace;

public class GetMarketplaceOffersMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetMarketplaceOffersMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetMarketplaceOffersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        (List<MarketplaceOfferSnapshot> offers, int totalFound) = await _grainFactory
            .GetMarketplaceSearchGrain()
            .GetOffersAsync(
                message.MinPrice,
                message.MaxPrice,
                message.SearchQuery,
                message.SortOrder,
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new MarketPlaceOffersEventMessageComposer
                {
                    Offers = offers,
                    TotalFound = totalFound,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
