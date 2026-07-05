using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.PacketHandlers.Configuration;
using Turbo.Primitives.Action;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Moderator;

public class ModMuteMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events,
    IOptions<ModerationConfig> moderationConfig
) : IMessageHandler<ModMuteMessage>
{
    private readonly ModerationConfig _moderationConfig = moderationConfig.Value;

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
            ActionContext actorCtx = ctx.AsActionContext() with { RoomId = targetRoomId };
            IRoomGrain roomGrain = grainFactory.GetRoomGrain(targetRoomId);
            int durationSeconds = (int)
                TimeSpan
                    .FromMinutes(_moderationConfig.ModToolDefaultMuteDurationMinutes)
                    .TotalSeconds;

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
