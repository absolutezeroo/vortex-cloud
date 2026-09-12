using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Marketplace;
using Vortex.Protocol.Messages.Outgoing.Marketplace;

namespace Vortex.PacketHandlers.Marketplace;

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
        {
            return;
        }

        // A safety-locked account does not spend. Answered as "not available" (AS3 2) rather than
        // with silence: the client's marketplace is already hidden while the lock is on, so an offer
        // bought from here is a client that does not honour it, and it still has to be told the
        // purchase did not happen or it will show the item as bought.
        if (
            await Catalog
                .SafetyLockGuard.IsLockedAsync(_grainFactory, ctx, ct)
                .ConfigureAwait(false)
        )
        {
            await ctx.SendComposerAsync(
                    new MarketplaceBuyOfferResultEventMessageComposer
                    {
                        Result = 2,
                        OfferId = message.OfferId,
                        NewPrice = 0,
                        OldOfferId = message.OfferId,
                    },
                    ct
                )
                .ConfigureAwait(false);

            return;
        }

        int result = await _grainFactory
            .GetMarketplacePurchaseGrain(ctx.PlayerId)
            .BuyOfferAsync(message.OfferId, ct)
            .ConfigureAwait(false);

        // grain: 0=ok, 1=not found, 2=no credits → AS3: 0=ok, 2=not available, 4=no credits
        int as3Result = result switch
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
            )
            .ConfigureAwait(false);
    }
}
