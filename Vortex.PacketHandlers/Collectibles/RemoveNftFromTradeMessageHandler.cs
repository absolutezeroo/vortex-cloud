using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Taking a Relic back off the trade table.
/// </summary>
/// <remarks>
/// The counterpart to <see cref="AddNftToTradeMessageHandler"/>, and it goes to the same owner: the
/// trade lives on the room grain with the furniture side of the offer. This only forwards.
/// </remarks>
public class RemoveNftFromTradeMessageHandler(IRoomService roomService)
    : IMessageHandler<RemoveNftFromTradeMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        RemoveNftFromTradeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .RemoveTradeAssetAsync(ctx.AsActionContext(), message.AssetId, ct)
            .ConfigureAwait(false);
    }
}
