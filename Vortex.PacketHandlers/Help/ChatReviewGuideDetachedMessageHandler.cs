using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// A guardian closing the review window. The others are not left waiting on a vote that is never
/// coming — dropping out can be what completes the review.
/// </summary>
public class ChatReviewGuideDetachedMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ChatReviewGuideDetachedMessage>
{
    public async ValueTask HandleAsync(
        ChatReviewGuideDetachedMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetGuideDirectoryGrain()
            .ChatReviewDetachAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);
    }
}
