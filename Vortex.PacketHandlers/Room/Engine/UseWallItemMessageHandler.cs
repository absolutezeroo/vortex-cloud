using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Room.Engine;

public class UseWallItemMessageHandler(IRoomService roomService)
    : IMessageHandler<UseWallItemMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        UseWallItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .UseItemInRoomAsync(ctx.AsActionContext(), message.ObjectId, ct, message.Param)
            .ConfigureAwait(false);
    }
}
