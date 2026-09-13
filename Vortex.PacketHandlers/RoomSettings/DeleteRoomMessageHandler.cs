using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Catalog;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.RoomSettings;

namespace Vortex.PacketHandlers.RoomSettings;

/// <summary>
/// Deleting a room, which the account safety lock stops.
/// </summary>
/// <remarks>
/// The client greys the link out for itself — <c>RoomSettingsCtrl.showDeleteButton()</c> disables
/// <c>remove_link_region</c> and drops both its label and its icon to half opacity when
/// <c>isAccountSafetyLocked()</c> — so a delete arriving here while locked comes from a client that
/// does not, which is exactly the case the lock exists for. A gate the client alone enforces
/// protects nobody from the person who stole the account.
/// </remarks>
public class DeleteRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<DeleteRoomMessage>
{
    public async ValueTask HandleAsync(
        DeleteRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        if (await SafetyLockGuard.IsLockedAsync(grainFactory, ctx, ct).ConfigureAwait(false))
        {
            return;
        }

        IRoomSettings roomGrain = grainFactory.GetRoomSettings(message.RoomId);
        await roomGrain.DeleteRoomAsync(ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
