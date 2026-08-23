using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Protocol.Messages.Incoming.Room.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Action;

public class MuteUserMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<MuteUserMessage>
{
    public async ValueTask HandleAsync(
        MuteUserMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.UserId <= 0 || message.Minutes <= 0)
        {
            return;
        }

        RoomId actorRoomId = message.RoomId > 0 ? new RoomId(message.RoomId) : ctx.RoomId;
        if (actorRoomId <= 0)
        {
            return;
        }

        ActionContext actorCtx = ctx.AsActionContext() with { RoomId = actorRoomId };
        // Room-scoped: the owner / rights-holders / guild members are authorized by the room's
        // own mod settings inside RoomGrain. All that is enforced here is that nobody sanctions
        // higher-ranked staff.
        if (
            !await RoomModerationGuard
                .CanActOnTargetAsync(
                    permissionService,
                    events,
                    ctx,
                    actorRoomId,
                    message.UserId,
                    ModerationAction.Mute,
                    ct
                )
                .ConfigureAwait(false)
        )
        {
            return;
        }

        IRoomModeration roomGrain = grainFactory.GetRoomModeration(actorRoomId);
        int durationSeconds = (int)Math.Ceiling(TimeSpan.FromMinutes(message.Minutes).TotalSeconds);

        bool applied = await roomGrain
            .MuteUserAsync(actorCtx, message.UserId, durationSeconds, ct)
            .ConfigureAwait(false);

        if (!applied)
        {
            // The room's mod settings did not grant this actor authority here.
            await RoomModerationGuard
                .AuditDenialAsync(
                    events,
                    ctx,
                    actorRoomId,
                    message.UserId,
                    ModerationAction.Mute,
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
