using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.RoomSettings;

namespace Turbo.PacketHandlers.RoomSettings;

public class GetCustomRoomFilterMessageHandler : IMessageHandler<GetCustomRoomFilterMessage>
{
    public ValueTask HandleAsync(
        GetCustomRoomFilterMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) => ValueTask.CompletedTask;
}
