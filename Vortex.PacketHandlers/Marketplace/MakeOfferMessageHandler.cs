using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Marketplace;
using Vortex.Protocol.Messages.Outgoing.Marketplace;

namespace Vortex.PacketHandlers.Marketplace;

public class MakeOfferMessageHandler(IGrainFactory grainFactory) : IMessageHandler<MakeOfferMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        MakeOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        // Listing is spending too — it costs the commission — and the client hides the whole
        // marketplace while the lock is on. AS3 5 is this message's own "error".
        if (
            await Catalog
                .SafetyLockGuard.IsLockedAsync(_grainFactory, ctx, ct)
                .ConfigureAwait(false)
        )
        {
            await ctx.SendComposerAsync(
                    new MarketplaceMakeOfferResultMessageComposer { Result = 5 },
                    ct
                )
                .ConfigureAwait(false);

            return;
        }

        (int result, _) = await _grainFactory
            .GetMarketplacePurchaseGrain(ctx.PlayerId)
            .MakeOfferAsync(message.FurnitureItemId, message.Price, ct)
            .ConfigureAwait(false);

        // result: 0=success(1 in AS3), 1=error(5 in AS3)
        int as3Result = result == 0 ? 1 : 5;

        await ctx.SendComposerAsync(
                new MarketplaceMakeOfferResultMessageComposer { Result = as3Result },
                ct
            )
            .ConfigureAwait(false);
    }
}
