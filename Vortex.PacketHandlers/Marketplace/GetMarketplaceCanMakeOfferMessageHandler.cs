using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Marketplace;
using Vortex.Primitives.Messages.Outgoing.Marketplace;

namespace Vortex.PacketHandlers.Marketplace;

public class GetMarketplaceCanMakeOfferMessageHandler
    : IMessageHandler<GetMarketplaceCanMakeOfferMessage>
{
    public async ValueTask HandleAsync(
        GetMarketplaceCanMakeOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ctx.SendComposerAsync(
                new MarketplaceCanMakeOfferResultMessageComposer { CanMakeOffer = true },
                ct
            )
            .ConfigureAwait(false);
    }
}
