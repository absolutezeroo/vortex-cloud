using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Furniture;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Room.Furniture;

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
