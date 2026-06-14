using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Marketplace;
using Turbo.Primitives.Messages.Outgoing.Marketplace;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Marketplace;

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
            return;

        var (offers, totalFound) = await _grainFactory
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
