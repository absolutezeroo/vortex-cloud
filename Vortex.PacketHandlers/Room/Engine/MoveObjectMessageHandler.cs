using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

public class MoveObjectMessageHandler(IRoomService roomService) : IMessageHandler<MoveObjectMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        MoveObjectMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .MoveFloorItemInRoomAsync(
                ctx.AsActionContext(),
                message.ObjectId,
                message.X,
                message.Y,
                message.Rotation,
                ct
            )
            .ConfigureAwait(false);
    }
}
