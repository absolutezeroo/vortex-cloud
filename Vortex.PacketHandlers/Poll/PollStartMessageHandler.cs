using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Poll;

namespace Vortex.PacketHandlers.Poll;

public class PollStartMessageHandler : IMessageHandler<PollStartMessage>
{
    public async ValueTask HandleAsync(
        PollStartMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
