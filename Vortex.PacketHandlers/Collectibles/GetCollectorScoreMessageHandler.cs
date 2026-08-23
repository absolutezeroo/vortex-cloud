using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.Collectibles;

public class GetCollectorScoreMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetCollectorScoreMessage>
{
    public async ValueTask HandleAsync(
        GetCollectorScoreMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        CollectorScoreSnapshot score = await grainFactory
            .GetNftCollectionsGrain()
            .GetCollectorScoreAsync(new PlayerId(ctx.PlayerId), ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new NftCollectionsScoreMessageComposer
                {
                    Score = score.Score,
                    HighestScore = score.HighestScore,
                    Level = score.Level,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
