using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Trading;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Inventory.Trading;

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
