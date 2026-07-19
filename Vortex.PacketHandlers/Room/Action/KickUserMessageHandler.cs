using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Messages.Incoming.Room.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Action;

public class KickUserMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<KickUserMessage>
{
    public async ValueTask HandleAsync(
        KickUserMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.UserId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        ActionContext actorCtx = ctx.AsActionContext();
        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);
        PermissionSet targetPermissions = await permissionService
            .ResolveForPlayerAsync(message.UserId, ct)
            .ConfigureAwait(false);

        if (!ModerationPolicy.IsAllowed(permissions, targetPermissions, ModerationAction.Kick))
        {
            await events
                .PublishAsync(
                    new ModerationActionDeniedEvent(
                        ctx.PlayerId,
                        message.UserId,
                        ctx.RoomId,
                        nameof(ModerationAction.Kick)
                    ),
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(ctx.RoomId);
        await roomGrain.KickUserAsync(actorCtx, message.UserId, ct).ConfigureAwait(false);
    }
}
