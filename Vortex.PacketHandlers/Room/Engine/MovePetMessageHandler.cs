using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

public class MovePetMessageHandler(IRoomService roomService) : IMessageHandler<MovePetMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        MovePetMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .MovePetInRoomAsync(
                ctx.AsActionContext(),
                message.PetId,
                message.X,
                message.Y,
                message.Rotation,
                ct
            )
            .ConfigureAwait(false);
    }
}
