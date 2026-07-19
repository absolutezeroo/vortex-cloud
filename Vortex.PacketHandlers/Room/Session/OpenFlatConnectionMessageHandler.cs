using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Session;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Room.Session;

public class OpenFlatConnectionMessageHandler(IRoomService roomService)
    : IMessageHandler<OpenFlatConnectionMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        OpenFlatConnectionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .OpenRoomForPlayerIdAsync(
                ctx.AsActionContext(),
                ctx.PlayerId,
                message.RoomId,
                ct,
                message.Password
            )
            .ConfigureAwait(false);
    }
}
