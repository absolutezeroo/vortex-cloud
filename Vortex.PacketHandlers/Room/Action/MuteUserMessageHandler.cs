using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Messages.Incoming.Room.Action;
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
        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);
        PermissionSet targetPermissions = await permissionService
            .ResolveForPlayerAsync(message.UserId, ct)
            .ConfigureAwait(false);

        if (!ModerationPolicy.IsAllowed(permissions, targetPermissions, ModerationAction.Mute))
        {
            await events
                .PublishAsync(
                    new ModerationActionDeniedEvent(
                        ctx.PlayerId,
                        message.UserId,
                        actorRoomId,
                        nameof(ModerationAction.Mute)
                    ),
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(actorRoomId);
        int durationSeconds = (int)Math.Ceiling(TimeSpan.FromMinutes(message.Minutes).TotalSeconds);

        await roomGrain
            .MuteUserAsync(actorCtx, message.UserId, durationSeconds, ct)
            .ConfigureAwait(false);
    }
}
