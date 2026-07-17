using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Trading;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Inventory.Trading;

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
