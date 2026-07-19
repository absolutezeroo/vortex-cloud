using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Trading;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Inventory.Trading;

public class RemoveItemFromTradeMessageHandler(IRoomService roomService)
    : IMessageHandler<RemoveItemFromTradeMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        RemoveItemFromTradeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .RemoveTradeItemAsync(ctx.AsActionContext(), message.ItemId, ct)
            .ConfigureAwait(false);
    }
}
