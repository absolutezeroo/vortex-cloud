using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// A guardian taking a chat review or passing on it. Taking it is what earns them the excerpt —
/// it is deliberately not sent with the offer, so declining never shows anyone the conversation.
/// </summary>
public class ChatReviewGuideDecidesOnOfferMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ChatReviewGuideDecidesOnOfferMessage>
{
    public async ValueTask HandleAsync(
        ChatReviewGuideDecidesOnOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ChatReviewOutcome outcome = await grainFactory
            .GetGuideDirectoryGrain()
            .ChatReviewDecideAsync(ctx.PlayerId, message.Accepted, ct)
            .ConfigureAwait(false);

        if (message.Accepted && outcome.ChatRecord.Length > 0)
        {
            await ChatReviewDispatch
                .SendRecordAsync(grainFactory, ctx.PlayerId, outcome.ChatRecord)
                .ConfigureAwait(false);
        }

        await ChatReviewDispatch.DeliverAsync(grainFactory, outcome, ct).ConfigureAwait(false);
    }
}
