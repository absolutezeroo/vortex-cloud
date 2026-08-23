using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

public class OpenMysteryTrophyMessageHandler(IRoomService roomService)
    : IMessageHandler<OpenMysteryTrophyMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        OpenMysteryTrophyMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .OpenMysteryTrophyAsync(
                ctx.AsActionContext(),
                message.ObjectId,
                message.Inscription,
                ct
            )
            .ConfigureAwait(false);
    }
}
