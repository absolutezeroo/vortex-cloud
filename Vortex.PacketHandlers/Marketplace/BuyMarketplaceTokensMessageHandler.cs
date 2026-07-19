using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Marketplace;

namespace Vortex.PacketHandlers.Marketplace;

public class BuyMarketplaceTokensMessageHandler : IMessageHandler<BuyMarketplaceTokensMessage>
{
    public async ValueTask HandleAsync(
        BuyMarketplaceTokensMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
