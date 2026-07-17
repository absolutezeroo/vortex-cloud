using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Trading;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Inventory.Trading;

public class AddItemsToTradeMessageHandler(IRoomService roomService)
    : IMessageHandler<AddItemsToTradeMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        AddItemsToTradeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .AddTradeItemsAsync(ctx.AsActionContext(), message.ItemIds, ct)
            .ConfigureAwait(false);
    }
}
