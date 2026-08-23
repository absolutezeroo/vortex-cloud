using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Marketplace;
using Vortex.Protocol.Messages.Outgoing.Marketplace;

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
        // 1 is "go ahead", which is what this hotel always answers -- there is no trading-pass or
        // token gate here. TokenCount only matters for result code 4 (buy tokens).
        await ctx.SendComposerAsync(
                new MarketplaceCanMakeOfferResultMessageComposer { ResultCode = 1 },
                ct
            )
            .ConfigureAwait(false);
    }
}
