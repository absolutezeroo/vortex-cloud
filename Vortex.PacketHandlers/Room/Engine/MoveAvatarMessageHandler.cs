using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Room.Engine;

public class MoveAvatarMessageHandler(IRoomService roomService) : IMessageHandler<MoveAvatarMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        MoveAvatarMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .ClickTileAsync(ctx.AsActionContext(), message.TargetX, message.TargetY, ct)
            .ConfigureAwait(false);
    }
}
