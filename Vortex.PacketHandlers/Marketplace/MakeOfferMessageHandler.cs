using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Marketplace;
using Vortex.Primitives.Messages.Outgoing.Marketplace;
using Vortex.Primitives.Orleans;

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
