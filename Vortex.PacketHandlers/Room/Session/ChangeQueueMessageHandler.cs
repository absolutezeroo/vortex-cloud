using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Session;

namespace Vortex.PacketHandlers.Room.Session;

public class ChangeQueueMessageHandler : IMessageHandler<ChangeQueueMessage>
{
    public async ValueTask HandleAsync(
        ChangeQueueMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
