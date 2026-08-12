using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Help;

public class GetQuizQuestionsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetQuizQuestionsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetQuizQuestionsMessage message,
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
            .RequestAsync(message.QuizCode, ct)
            .ConfigureAwait(false);
    }
}
