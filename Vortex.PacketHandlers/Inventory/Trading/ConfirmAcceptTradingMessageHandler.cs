using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Trading;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Inventory.Trading;

public class ConfirmAcceptTradingMessageHandler(IRoomService roomService)
    : IMessageHandler<ConfirmAcceptTradingMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        ConfirmAcceptTradingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService.ConfirmTradeAsync(ctx.AsActionContext(), true, ct).ConfigureAwait(false);
    }
}
