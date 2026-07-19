using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Catalog;

public class ChargeFireworkMessageHandler(IRoomService roomService)
    : IMessageHandler<ChargeFireworkMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        ChargeFireworkMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .UseItemInRoomAsync(ctx.AsActionContext(), message.SpriteId, ct, message.Type)
            .ConfigureAwait(false);
    }
}
