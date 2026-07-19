using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

public class GetCollectibleWalletAddressesMessageHandler
    : IMessageHandler<GetCollectibleWalletAddressesMessage>
{
    public async ValueTask HandleAsync(
        GetCollectibleWalletAddressesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
