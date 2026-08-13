using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Mint tokens held. None: they are bought to mint against a chain this hotel does not have.
/// </summary>
/// <remarks>
/// Answering matters more than the answer. This handler used to return without sending anything,
/// and the collectibles interface waits on every one of these -- so silence left it loading rather
/// than showing that the feature is off.
/// </remarks>
public class GetCollectibleMintTokensMessageHandler
    : IMessageHandler<GetCollectibleMintTokensMessage>
{
    public async ValueTask HandleAsync(
        GetCollectibleMintTokensMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(new CollectibleMintTokenCountMessageComposer { Count = 0 }, ct)
            .ConfigureAwait(false);
}
