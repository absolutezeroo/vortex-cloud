using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Help;

public class PostQuizAnswersMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<PostQuizAnswersMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        PostQuizAnswersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetPlayerQuizGrain(ctx.PlayerId)
            .SubmitAsync(message.QuizCode, message.Answers, ct)
            .ConfigureAwait(false);
    }
}
