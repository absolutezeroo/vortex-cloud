using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Navigator;

/// <summary>Cancels a purchased room advertisement early. Despite the "Event" naming (matching the
/// client's own legacy terminology), this operates on the same RoomAdvertisementEntity that
/// PurchaseRoomAdMessage creates -- see AGENTS.md room-ads notes.</summary>
public class CancelEventMessageHandler(IRoomAdvertisementService roomAdvertisements)
    : IMessageHandler<CancelEventMessage>
{
    public async ValueTask HandleAsync(
        CancelEventMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.AdvertisementId <= 0)
        {
            return;
        }

        RoomId? roomId = await roomAdvertisements
            .CancelAsync(message.AdvertisementId, ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (roomId is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new RoomEventCancelMessageComposer { RoomId = roomId.Value },
                ct
            )
            .ConfigureAwait(false);
    }
}
