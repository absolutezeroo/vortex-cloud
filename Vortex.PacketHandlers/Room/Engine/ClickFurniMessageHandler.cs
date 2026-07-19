using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Room.Engine;

public class ClickFurniMessageHandler(IRoomService roomService) : IMessageHandler<ClickFurniMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        ClickFurniMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        bool isFloorItemClicked = message.ObjectId > 0;
        bool isWallItemClicked = message.ObjectId < 0;

        if (isFloorItemClicked)
        {
            await _roomService
                .ClickItemInRoomAsync(ctx.AsActionContext(), message.ObjectId, ct, message.Param)
                .ConfigureAwait(false);
        }
        else if (isWallItemClicked)
        {
            await _roomService
                .ClickItemInRoomAsync(ctx.AsActionContext(), message.ObjectId, ct, message.Param)
                .ConfigureAwait(false);
        }
    }
}
