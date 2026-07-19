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

public class UnmuteUserMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<UnmuteUserMessage>
{
    public async ValueTask HandleAsync(
        UnmuteUserMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.UserId <= 0)
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

        if (!ModerationPolicy.IsAllowed(permissions, targetPermissions, ModerationAction.Mute))
        {
            await events
                .PublishAsync(
                    new ModerationActionDeniedEvent(
                        ctx.PlayerId,
                        message.UserId,
                        ctx.RoomId,
                        nameof(ModerationAction.Mute)
                    ),
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(ctx.RoomId);
        await roomGrain.UnmuteUserAsync(actorCtx, message.UserId, ct).ConfigureAwait(false);
    }
}
