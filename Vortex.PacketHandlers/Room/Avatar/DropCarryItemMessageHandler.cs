using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Avatar;

namespace Vortex.PacketHandlers.Room.Avatar;

public class DropCarryItemMessageHandler : IMessageHandler<DropCarryItemMessage>
{
    public async ValueTask HandleAsync(
        DropCarryItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
