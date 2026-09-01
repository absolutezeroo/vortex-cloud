using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Moderator;

namespace Vortex.PacketHandlers.Moderator;

/// <summary>
/// The room-tool checkboxes. No in-room controller level gates them, so what stands between a
/// player and someone else's room is <c>Capabilities.Room.ModerateAny</c> — not the per-room
/// kick/ban settings. The grain resolves it too, and that is the check that decides; this one is
/// here to answer the client without waking a room.
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
