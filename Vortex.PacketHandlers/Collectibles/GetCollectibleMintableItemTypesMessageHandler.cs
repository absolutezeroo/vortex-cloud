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
/// What may be converted into a Relic.
/// </summary>
/// <remarks>
/// The client fills the minting grid from this list and then counts, for each row, how many copies
/// the player holds — by sprite id, in the floor or wall inventory depending on the kind byte. A
/// type whose window has closed is left out here rather than sent and greyed out, because the client
/// gives no reason for a greyed-out row.
/// </remarks>
public class GetCollectibleMintableItemTypesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetCollectibleMintableItemTypesMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetCollectibleMintableItemTypesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        ImmutableArray<MintableItemTypeSnapshot> types = await _grainFactory
            .GetNftMintingGrain()
            .GetMintableItemTypesAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new CollectableMintableItemTypesMessageComposer { ItemTypes = types },
                ct
            )
            .ConfigureAwait(false);
    }
}
