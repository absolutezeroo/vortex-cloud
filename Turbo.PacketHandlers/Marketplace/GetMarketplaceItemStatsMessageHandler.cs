using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Marketplace;
using Turbo.Primitives.Messages.Outgoing.Marketplace;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Marketplace;

public class GetMarketplaceItemStatsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetMarketplaceItemStatsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetMarketplaceItemStatsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        var stats = await _grainFactory
            .GetMarketplaceSearchGrain()
            .GetItemStatsAsync(message.TypeId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
            new MarketplaceItemStatsEventMessageComposer
            {
                AvgPrice = stats.AvgPrice,
                OfferCount = stats.OfferCount,
                CategoryId = message.CategoryId,
                TypeId = message.TypeId,
            },
            ct
        ).ConfigureAwait(false);
    }
}
