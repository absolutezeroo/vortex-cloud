using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.RoomSettings;

namespace Vortex.PacketHandlers.RoomSettings;

public class UpdateRoomFilterMessageHandler : IMessageHandler<UpdateRoomFilterMessage>
{
    public ValueTask HandleAsync(
        UpdateRoomFilterMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) => ValueTask.CompletedTask;
}
