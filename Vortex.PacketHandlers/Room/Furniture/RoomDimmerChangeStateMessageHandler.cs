using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

public class RoomDimmerChangeStateMessageHandler : IMessageHandler<RoomDimmerChangeStateMessage>
{
    public async ValueTask HandleAsync(
        RoomDimmerChangeStateMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
