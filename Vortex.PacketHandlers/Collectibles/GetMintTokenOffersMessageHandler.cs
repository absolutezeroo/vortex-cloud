using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// The stamp bundles on sale, which fill the minting tab's dropdown.
/// </summary>
public class GetMintTokenOffersMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetMintTokenOffersMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetMintTokenOffersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        ImmutableArray<MintTokenOfferSnapshot> offers = await _grainFactory
            .GetNftMintingGrain()
            .GetTokenOffersAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new CollectibleMintTokenOffersMessageComposer { Offers = offers },
                ct
            )
            .ConfigureAwait(false);
    }
}
