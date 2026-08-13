using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// What the Collectors Guild shop has for sale.
/// </summary>
/// <remarks>
/// This used to answer an empty list on the grounds that an offer is minted against a chain the
/// hotel does not have. That was wrong: an offer is a furniture classname, a price in emeralds and
/// two flags, none of which needs a chain — the same reason collections work here. The shop is real
/// and admin-filled; an empty list now means the shelf is empty rather than the feature is off.
/// </remarks>
public class GetNftStoreOffersMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetNftStoreOffersMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetNftStoreOffersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        ImmutableArray<NftStoreOfferSnapshot> offers = await _grainFactory
            .GetNftStoreGrain()
            .GetOffersAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new NftStoreOffersMessageComposer { Offers = offers }, ct)
            .ConfigureAwait(false);
    }
}
