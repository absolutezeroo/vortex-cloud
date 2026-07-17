using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Trading;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Inventory.Trading;

public class UnacceptTradingMessageHandler(IRoomService roomService)
    : IMessageHandler<UnacceptTradingMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        UnacceptTradingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .SetTradeAcceptAsync(ctx.AsActionContext(), false, ct)
            .ConfigureAwait(false);
    }
}
