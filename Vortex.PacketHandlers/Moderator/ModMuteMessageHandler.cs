using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Server.Grains;

namespace Vortex.PacketHandlers.Moderator;

public class ModMuteMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<ModMuteMessage>
{
    public async ValueTask HandleAsync(
        ModMuteMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.UserId <= 0)
        {
            return;
        }

        RoomId targetRoomId = await ModToolActionHelper
            .GetTargetRoomIdAsync(grainFactory, message.UserId)
            .ConfigureAwait(false);

        bool success = false;

        if (
            await ModToolActionHelper
                .IsAuthorizedAsync(
                    permissionService,
                    events,
                    ctx.PlayerId,
                    message.UserId,
                    targetRoomId,
                    ModerationAction.Mute,
                    ct
                )
                .ConfigureAwait(false)
            && targetRoomId > 0
        )
        {
            int muteMinutes = await grainFactory
                .GetServerConfigGrain()
                .GetIntAsync(
                    ModerationConfig.ModToolDefaultMuteMinutesKey,
                    ModerationConfig.ModToolDefaultMuteMinutesDefault
                )
                .ConfigureAwait(false);

            ActionContext actorCtx = ctx.AsActionContext() with { RoomId = targetRoomId };
            IRoomGrain roomGrain = grainFactory.GetRoomGrain(targetRoomId);
            int durationSeconds = (int)TimeSpan.FromMinutes(muteMinutes).TotalSeconds;

            success = await roomGrain
                .MuteUserAsync(actorCtx, message.UserId, durationSeconds, ct)
                .ConfigureAwait(false);
        }

        await ModToolActionHelper
            .SendCautionIfPresentAsync(grainFactory, message.UserId, message.Message)
            .ConfigureAwait(false);
        await ModToolActionHelper
            .SendResultAsync(grainFactory, ctx.PlayerId, message.UserId, success)
            .ConfigureAwait(false);
    }
}
