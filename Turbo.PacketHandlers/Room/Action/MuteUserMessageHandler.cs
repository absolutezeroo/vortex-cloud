using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Incoming.Room.Action;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Room.Action;

public class MuteUserMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<MuteUserMessage>
{
    public async ValueTask HandleAsync(MuteUserMessage message, MessageContext ctx, CancellationToken ct)
    {
        if (ctx.PlayerId <= 0 || message.UserId <= 0 || message.Minutes <= 0)
            return;

        RoomId actorRoomId = message.RoomId > 0 ? new RoomId(message.RoomId) : ctx.RoomId;
        if (actorRoomId <= 0)
            return;

        var actorCtx = ctx.AsActionContext() with { RoomId = actorRoomId };
        var permissions = await permissionService.ResolveForPlayerAsync(ctx.PlayerId, ct).ConfigureAwait(
            false
        );

        if (!ModerationPolicy.IsAllowed(permissions, ModerationAction.Mute))
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

        var roomGrain = grainFactory.GetRoomGrain(actorRoomId);
        var durationSeconds = (int)Math.Ceiling(TimeSpan.FromMinutes(message.Minutes).TotalSeconds);

        await roomGrain
            .MuteUserAsync(actorCtx, message.UserId, durationSeconds, ct)
            .ConfigureAwait(false);
    }
}
