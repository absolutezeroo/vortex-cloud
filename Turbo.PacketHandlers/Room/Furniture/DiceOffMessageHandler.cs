using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Furniture;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Object.Logic.Furniture;

namespace Turbo.PacketHandlers.Room.Furniture;

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
