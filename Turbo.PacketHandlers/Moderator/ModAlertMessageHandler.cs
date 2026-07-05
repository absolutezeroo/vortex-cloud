using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Moderator;

public class ModAlertMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<ModAlertMessage>
{
    public async ValueTask HandleAsync(
        ModAlertMessage message,
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

        bool success = await ModToolActionHelper
            .IsAuthorizedAsync(
                permissionService,
                events,
                ctx.PlayerId,
                message.UserId,
                targetRoomId,
                ModerationAction.Alert,
                ct
            )
            .ConfigureAwait(false);

        if (success)
        {
            await ModToolActionHelper
                .SendCautionIfPresentAsync(grainFactory, message.UserId, message.Message)
                .ConfigureAwait(false);

            await events
                .PublishAsync(
                    new PlayerAlertedEvent(ctx.PlayerId, message.UserId, targetRoomId),
                    ct
                )
                .ConfigureAwait(false);
        }

        await ModToolActionHelper
            .SendResultAsync(grainFactory, ctx.PlayerId, message.UserId, success)
            .ConfigureAwait(false);
    }
}
