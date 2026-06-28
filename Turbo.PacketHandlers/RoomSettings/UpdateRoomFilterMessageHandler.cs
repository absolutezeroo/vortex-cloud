using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.RoomSettings;

namespace Turbo.PacketHandlers.RoomSettings;

public class UpdateRoomFilterMessageHandler : IMessageHandler<UpdateRoomFilterMessage>
{
    public ValueTask HandleAsync(
        UpdateRoomFilterMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) => ValueTask.CompletedTask;
}
