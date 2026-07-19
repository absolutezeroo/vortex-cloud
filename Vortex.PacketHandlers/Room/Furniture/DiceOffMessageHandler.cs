using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

public class DiceOffMessageHandler(IRoomService roomService) : IMessageHandler<DiceOffMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        DiceOffMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .UseItemInRoomAsync(
                ctx.AsActionContext(),
                message.ObjectId,
                ct,
                FurnitureDiceAction.TurnOff
            )
            .ConfigureAwait(false);
    }
}
