using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Help;

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
