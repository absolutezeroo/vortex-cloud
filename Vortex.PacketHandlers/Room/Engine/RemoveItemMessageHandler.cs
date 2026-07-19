using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

public class RemoveItemMessageHandler : IMessageHandler<RemoveItemMessage>
{
    public async ValueTask HandleAsync(
        RemoveItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
