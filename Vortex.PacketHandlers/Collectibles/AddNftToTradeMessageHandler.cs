using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Putting Relics on the trade table.
/// </summary>
/// <remarks>
/// The trade lives on the room grain with the furniture side of the same offer, which is what lets
/// a chair and a Relic change hands in one transaction. This only forwards.
/// </remarks>
public class AddNftToTradeMessageHandler(IRoomService roomService)
    : IMessageHandler<AddNftToTradeMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        AddNftToTradeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .AddTradeAssetsAsync(ctx.AsActionContext(), message.AssetIds, ct)
            .ConfigureAwait(false);
    }
}
