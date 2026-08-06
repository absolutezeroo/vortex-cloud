using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Moderator;

/// <summary>
/// The mod tool's "send message to user" action. Distinct from ModAlert: this one renders as a
/// plain staff message rather than a caution the user has to acknowledge, so it carries no sanction
/// weight — but it still reaches a specific player, hence the same Alert gate.
/// </summary>
public class ModMessageMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<ModMessageMessage>
{
    public async ValueTask HandleAsync(
        ModMessageMessage message,
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

        bool success =
            !string.IsNullOrWhiteSpace(message.Message)
            && await ModToolActionHelper
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
            await grainFactory
                .GetPlayerPresenceGrain(message.UserId)
                .SendComposerAsync(new ModeratorMessageComposer { Message = message.Message })
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
