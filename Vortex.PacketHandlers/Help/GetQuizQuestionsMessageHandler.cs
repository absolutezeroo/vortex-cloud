using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

public class GetQuizQuestionsMessageHandler : IMessageHandler<GetQuizQuestionsMessage>
{
    public async ValueTask HandleAsync(
        GetQuizQuestionsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
