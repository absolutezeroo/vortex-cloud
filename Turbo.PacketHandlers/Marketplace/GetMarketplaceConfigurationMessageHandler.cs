using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Marketplace;
using Turbo.Primitives.Messages.Outgoing.Marketplace;

namespace Turbo.PacketHandlers.Marketplace;

public class GetMarketplaceConfigurationMessageHandler
    : IMessageHandler<GetMarketplaceConfigurationMessage>
{
    public async ValueTask HandleAsync(
        GetMarketplaceConfigurationMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ctx.SendComposerAsync(
            new MarketplaceConfigurationEventMessageComposer
            {
                Enabled = true,
                Commission = 1,
                Credits = 0,
                Advertisements = 0,
                MinimumPrice = 1,
                MaximumPrice = 9999999,
                OfferTime = 259200,
                DisplayTime = 259200,
            },
            ct
        ).ConfigureAwait(false);
    }
}
