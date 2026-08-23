using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

public class GetNftCollectionsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetNftCollectionsMessage>
{
    public async ValueTask HandleAsync(
        GetNftCollectionsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ImmutableArray<NftCollectionSnapshot> collections = await grainFactory
            .GetNftCollectionsGrain()
            .GetCollectionsForPlayerAsync(new PlayerId(ctx.PlayerId), ct)
            .ConfigureAwait(false);

        // Sent even when the hotel runs no collections: the client draws an empty shelf, whereas
        // silence leaves the tab spinning.
        await ctx.SendComposerAsync(
                new NftCollectionsMessageComposer { Collections = collections },
                ct
            )
            .ConfigureAwait(false);
    }
}
