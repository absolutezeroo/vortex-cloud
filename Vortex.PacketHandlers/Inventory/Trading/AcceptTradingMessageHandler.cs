using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Trading;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Inventory.Trading;

public class AcceptTradingMessageHandler(IRoomService roomService)
    : IMessageHandler<AcceptTradingMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        AcceptTradingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .SetTradeAcceptAsync(ctx.AsActionContext(), true, ct)
            .ConfigureAwait(false);
    }
}
