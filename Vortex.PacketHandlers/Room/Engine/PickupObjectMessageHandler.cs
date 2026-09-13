using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Catalog;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Room.Engine;
using Vortex.Protocol.Messages.Outgoing.Notifications;

namespace Vortex.PacketHandlers.Room.Engine;

/// <summary>
/// Taking a piece of furniture back out of a room.
/// </summary>
/// <remarks>
/// <para>
/// This is the one thing the account safety lock stops that the CLIENT does not grey out for
/// itself. Its four gates are all elsewhere — the catalog's buy and gift buttons
/// (<c>PurchaseCatalogWidget.enableBuyButton</c>/<c>enableGiftButton</c>), the marketplace
/// (<c>MarketPlaceCatalogWidget</c>), the inventory's sell button (<c>FurniView</c>) and the
/// delete-room link (<c>RoomSettingsCtrl.showDeleteButton</c>) — so a locked account's pickup
/// arrives here looking exactly like any other, and only the server can refuse it. That also makes
/// it the most important of the five: emptying the rooms is what a thief does when they cannot
/// sell.
/// </para>
/// </remarks>
public class PickupObjectMessageHandler(IRoomService roomService, IGrainFactory grainFactory)
    : IMessageHandler<PickupObjectMessage>
{
    private readonly IRoomService _roomService = roomService;
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        PickupObjectMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (await SafetyLockGuard.IsLockedAsync(_grainFactory, ctx, ct).ConfigureAwait(false))
        {
            // `HabboNotifications.showNotification(type, params)` resolves `notification.<type>`
            // out of external_variables for the display mode, and the text out of
            // `notification.<type>.message`. The hotel's asset host needs that pair for the bubble
            // to draw; without it the pickup is still refused, silently — which is the half that
            // protects the account, and the one that must not wait on a data file.
            await ctx.SendComposerAsync(
                    new NotificationDialogMessageComposer
                    {
                        Type = "safety_locked",
                        Parameters = ImmutableArray<NotificationDialogParameter>.Empty,
                    },
                    ct
                )
                .ConfigureAwait(false);

            return;
        }

        int categoryId = message.CategoryId;

        if (categoryId == 1)
        {
            await _roomService
                .PickupItemInRoomAsync(ctx.AsActionContext(), message.ObjectId, ct, message.Confirm)
                .ConfigureAwait(false);
            return;
        }

        if (categoryId == 2)
        {
            await _roomService
                .PickupItemInRoomAsync(ctx.AsActionContext(), message.ObjectId, ct, message.Confirm)
                .ConfigureAwait(false);
        }
    }
}
