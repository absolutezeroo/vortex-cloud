using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Inventory.Trading;

namespace Vortex.PacketHandlers.Inventory.Trading;

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
