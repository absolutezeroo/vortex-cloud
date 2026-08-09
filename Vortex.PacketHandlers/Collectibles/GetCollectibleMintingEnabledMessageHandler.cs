using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Minting turns a piece of furniture into a token on a chain. An emulator has no chain, no wallet
/// and no contract, so it says no — and the client puts the whole minting half of the collectibles
/// interface away rather than offering buttons that cannot work.
/// </summary>
public class GetCollectibleMintingEnabledMessageHandler
    : IMessageHandler<GetCollectibleMintingEnabledMessage>
{
    public async ValueTask HandleAsync(
        GetCollectibleMintingEnabledMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(
                new CollectibleMintingEnabledMessageComposer { Enabled = false },
                ct
            )
            .ConfigureAwait(false);
}
