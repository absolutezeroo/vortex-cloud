using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Trading;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Inventory.Trading;

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
