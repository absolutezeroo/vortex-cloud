using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Incoming.Room.Action;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;

namespace Turbo.PacketHandlers.Room.Action;

public class UnbanUserFromRoomMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<UnbanUserFromRoomMessage>
{
    public async ValueTask HandleAsync(UnbanUserFromRoomMessage message, MessageContext ctx, CancellationToken ct)
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.UserId <= 0)
            return;

        var actorCtx = ctx.AsActionContext();
        var permissions = await permissionService.ResolveForPlayerAsync(ctx.PlayerId, ct).ConfigureAwait(
            false
        );

        if (!ModerationPolicy.IsAllowed(permissions, ModerationAction.Ban))
        {
            await events
                .PublishAsync(
                    new ModerationActionDeniedEvent(
                        ctx.PlayerId,
                        message.UserId,
                        ctx.RoomId,
                        nameof(ModerationAction.Ban)
                    ),
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        var roomGrain = grainFactory.GetRoomGrain(ctx.RoomId);
        await roomGrain
            .UnbanUserAsync(actorCtx, message.UserId, ct)
            .ConfigureAwait(false);
    }
}
