using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// The collectibles shop tab. Nothing is for sale here: the offers are minted against a chain this
/// hotel does not have, the same reason minting itself answers disabled.
/// </summary>
/// <remarks>
/// Answering with an empty list rather than not answering. The tab raises a waiting flag when it
/// asks and clears it only on this reply, so a dropped request leaves it spinning for as long as it
/// stays open — where an empty list makes it render nothing and mark itself ready, which is the
/// truth.
/// </remarks>
public class GetNftStoreOffersMessageHandler : IMessageHandler<GetNftStoreOffersMessage>
{
    public async ValueTask HandleAsync(
        GetNftStoreOffersMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(
                new NftStoreOffersMessageComposer
                {
                    Offers = ImmutableArray<NftStoreOfferSnapshot>.Empty,
                },
                ct
            )
            .ConfigureAwait(false);
}
