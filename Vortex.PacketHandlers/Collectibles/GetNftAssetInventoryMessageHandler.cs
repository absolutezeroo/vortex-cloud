using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Networking;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// The player's collectible assets. There are none: an asset is a token held in a wallet on a chain
/// this hotel does not have.
/// </summary>
/// <remarks>
/// The empty list is the whole point. This handler used to return without sending anything, which
/// left the inventory's Collectibles tab on its loading state permanently — that state is simply
/// "the list was never initialised", and only this reply initialises it. It also used to carry the
/// documentation of a different message, the wallet transfer, which is why it read as deliberate.
/// </remarks>
public class GetNftAssetInventoryMessageHandler : IMessageHandler<GetNftAssetInventoryMessage>
{
    public async ValueTask HandleAsync(
        GetNftAssetInventoryMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(
                new TradeNftAssetInventoryMessageComposer
                {
                    Assets = ImmutableArray<CollectibleAssetSnapshot>.Empty,
                },
                ct
            )
            .ConfigureAwait(false);
}
