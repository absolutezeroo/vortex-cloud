using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Marketplace;
using Turbo.Primitives.Messages.Outgoing.Marketplace;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Marketplace;

public class BuyMarketplaceOfferMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<BuyMarketplaceOfferMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        BuyMarketplaceOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        var result = await _grainFactory
            .GetMarketplacePurchaseGrain(ctx.PlayerId)
            .BuyOfferAsync(message.OfferId, ct)
            .ConfigureAwait(false);

        // grain: 0=ok, 1=not found, 2=no credits → AS3: 0=ok, 2=not available, 4=no credits
        var as3Result = result switch
        {
            0 => 0,
            2 => 4,
            _ => 2,
        };

        await ctx.SendComposerAsync(
            new MarketplaceBuyOfferResultEventMessageComposer
            {
                Result = as3Result,
                OfferId = message.OfferId,
                NewPrice = 0,
                OldOfferId = message.OfferId,
            },
            ct
        ).ConfigureAwait(false);
    }
}
