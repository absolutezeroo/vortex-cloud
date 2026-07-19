using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Trading;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Inventory.Trading;

public class OpenTradingMessageHandler(IRoomService roomService)
    : IMessageHandler<OpenTradingMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        OpenTradingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .OpenTradeAsync(ctx.AsActionContext(), message.OtherUserRoomObjectId, ct)
            .ConfigureAwait(false);
    }
}
