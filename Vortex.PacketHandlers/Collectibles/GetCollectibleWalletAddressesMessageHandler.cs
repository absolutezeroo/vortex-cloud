using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Which wallets the player has linked: none, and there is nowhere to link one. Answered rather
/// than ignored because the client waits on it before it will draw the collections tab at all.
/// </summary>
public class GetCollectibleWalletAddressesMessageHandler
    : IMessageHandler<GetCollectibleWalletAddressesMessage>
{
    public async ValueTask HandleAsync(
        GetCollectibleWalletAddressesMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(new CollectibleWalletAddressesMessageComposer(), ct)
            .ConfigureAwait(false);
}
