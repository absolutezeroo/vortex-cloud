using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Catalog;

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
