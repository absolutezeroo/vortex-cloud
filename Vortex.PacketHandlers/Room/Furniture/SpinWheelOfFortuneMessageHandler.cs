using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

public class SpinWheelOfFortuneMessageHandler(IRoomService roomService)
    : IMessageHandler<SpinWheelOfFortuneMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        SpinWheelOfFortuneMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .UseItemInRoomAsync(ctx.AsActionContext(), message.ObjectId, ct)
            .ConfigureAwait(false);
    }
}
