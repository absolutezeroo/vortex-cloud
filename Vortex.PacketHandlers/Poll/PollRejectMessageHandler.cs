using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Poll;

namespace Vortex.PacketHandlers.Poll;

public class PollRejectMessageHandler : IMessageHandler<PollRejectMessage>
{
    public async ValueTask HandleAsync(
        PollRejectMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
