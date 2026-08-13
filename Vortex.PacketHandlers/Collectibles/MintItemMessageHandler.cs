using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Minting a piece of furniture into a token. Refused: no chain, no contract.
/// </summary>
/// <remarks>
/// Answering matters more than the answer. This handler used to return without sending anything,
/// and the collectibles interface waits on every one of these -- so silence left it loading rather
/// than showing that the feature is off.
/// </remarks>
public class MintItemMessageHandler : IMessageHandler<MintItemMessage>
{
    public async ValueTask HandleAsync(
        MintItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(
                new CollectibleMintableItemResultMessageComposer
                {
                    Status = CollectibleMintableItemResultMessageComposer.NotAvailable,
                },
                ct
            )
            .ConfigureAwait(false);
}
