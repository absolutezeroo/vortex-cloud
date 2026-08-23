using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Moderator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Moderator;

/// <summary>
/// The room-tool checkboxes. The room grain applies them without consulting any in-room controller
/// level, so this gate is the only thing standing between a player and someone else's room —
/// <c>Capabilities.Room.ModerateAny</c>, not the per-room kick/ban settings.
/// </summary>
public class ModerateRoomMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService
) : IMessageHandler<ModerateRoomMessage>
{
    public async ValueTask HandleAsync(
        ModerateRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.Has(Capabilities.Room.ModerateAny))
        {
            return;
        }

        RoomId roomId = message.RoomId;

        await grainFactory
            .GetRoomModeration(roomId)
            .ApplyStaffRoomActionsAsync(
                ctx.PlayerId,
                message.LockDoor,
                message.ChangeName,
                message.KickUsers,
                ct
            )
            .ConfigureAwait(false);
    }
}
