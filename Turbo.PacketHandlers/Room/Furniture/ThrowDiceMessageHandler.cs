using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Furniture;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Object.Logic.Furniture;

namespace Turbo.PacketHandlers.Room.Furniture;

public class ThrowDiceMessageHandler(IRoomService roomService) : IMessageHandler<ThrowDiceMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        ThrowDiceMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .UseItemInRoomAsync(
                ctx.AsActionContext(),
                message.ObjectId,
                ct,
                FurnitureDiceAction.Roll
            )
            .ConfigureAwait(false);
    }
}
