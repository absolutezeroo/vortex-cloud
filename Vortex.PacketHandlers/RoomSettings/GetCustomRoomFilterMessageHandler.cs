using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.RoomSettings;

namespace Vortex.PacketHandlers.RoomSettings;

public class GetCustomRoomFilterMessageHandler : IMessageHandler<GetCustomRoomFilterMessage>
{
    public ValueTask HandleAsync(
        GetCustomRoomFilterMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) => ValueTask.CompletedTask;
}
