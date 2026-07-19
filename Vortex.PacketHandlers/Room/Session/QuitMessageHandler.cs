using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Session;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Room.Session;

public class QuitMessageHandler(IRoomService roomService) : IMessageHandler<QuitMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        QuitMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService.CloseRoomForPlayerAsync(ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
