using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Marketplace;
using Turbo.Primitives.Messages.Outgoing.Marketplace;

namespace Turbo.PacketHandlers.Marketplace;

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
