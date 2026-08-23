using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Inventory.Trading;

namespace Vortex.PacketHandlers.Inventory.Trading;

public class AddItemToTradeMessageHandler(IRoomService roomService)
    : IMessageHandler<AddItemToTradeMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        AddItemToTradeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .AddTradeItemsAsync(ctx.AsActionContext(), [message.ItemId], ct)
            .ConfigureAwait(false);
    }
}
