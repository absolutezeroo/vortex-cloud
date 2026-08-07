using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Help;

/// <summary>A guardian's verdict on the excerpt they took.</summary>
public class ChatReviewGuideVoteMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ChatReviewGuideVoteMessage>
{
    public async ValueTask HandleAsync(
        ChatReviewGuideVoteMessage message,
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
            .ChatReviewVoteAsync(ctx.PlayerId, message.Vote, ct)
            .ConfigureAwait(false);
    }
}
