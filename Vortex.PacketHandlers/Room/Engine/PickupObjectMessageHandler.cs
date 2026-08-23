using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

public class PickupObjectMessageHandler(IRoomService roomService)
    : IMessageHandler<PickupObjectMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        PickupObjectMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        int categoryId = message.CategoryId;

        if (categoryId == 1)
        {
            await _roomService
                .PickupItemInRoomAsync(ctx.AsActionContext(), message.ObjectId, ct, message.Confirm)
                .ConfigureAwait(false);
            return;
        }

        if (categoryId == 2)
        {
            await _roomService
                .PickupItemInRoomAsync(ctx.AsActionContext(), message.ObjectId, ct, message.Confirm)
                .ConfigureAwait(false);
        }
    }
}
