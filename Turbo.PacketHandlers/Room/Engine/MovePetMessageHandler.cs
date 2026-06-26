using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Engine;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Room.Engine;

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
