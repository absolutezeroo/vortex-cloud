using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Buying mint tokens. Nothing is sold, so the balance comes back unchanged -- the same composer the balance query uses, which is what the view re-reads.
/// </summary>
/// <remarks>
/// Answering matters more than the answer. This handler used to return without sending anything,
/// and the collectibles interface waits on every one of these -- so silence left it loading rather
/// than showing that the feature is off.
/// </remarks>
public class PurchaseMintTokenMessageHandler : IMessageHandler<PurchaseMintTokenMessage>
{
    public async ValueTask HandleAsync(
        PurchaseMintTokenMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(new CollectibleMintTokenCountMessageComposer { Count = 0 }, ct)
            .ConfigureAwait(false);
}
