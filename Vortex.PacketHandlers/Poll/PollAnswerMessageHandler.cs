using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Poll;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Poll;

public class PollAnswerMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<PollAnswerMessage>
{
    public async ValueTask HandleAsync(
        PollAnswerMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        // An empty answer list is the client skipping a question, not a payload to store.
        if (message.Answers.IsDefaultOrEmpty)
        {
            return;
        }

        await grainFactory
            .GetPlayerPollGrain(ctx.PlayerId)
            .AnswerAsync(message.PollId, message.QuestionId, message.Answers, ct)
            .ConfigureAwait(false);
    }
}
