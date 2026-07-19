using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Trading;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Inventory.Trading;

public class ConfirmDeclineTradingMessageHandler(IRoomService roomService)
    : IMessageHandler<ConfirmDeclineTradingMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        ConfirmDeclineTradingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .ConfirmTradeAsync(ctx.AsActionContext(), false, ct)
            .ConfigureAwait(false);
    }
}
