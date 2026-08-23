using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Inventory.Trading;

namespace Vortex.PacketHandlers.Inventory.Trading;

public class CloseTradingMessageHandler(IRoomService roomService)
    : IMessageHandler<CloseTradingMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        CloseTradingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService.CloseTradeAsync(ctx.AsActionContext(), ct).ConfigureAwait(false);
    }
}
