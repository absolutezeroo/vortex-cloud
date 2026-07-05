using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Catalog;

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
