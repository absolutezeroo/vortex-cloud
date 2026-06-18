using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Incoming.Room.Action;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;

namespace Turbo.PacketHandlers.Room.Action;

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
            return;

        var actorCtx = ctx.AsActionContext();
        var permissions = await permissionService.ResolveForPlayerAsync(ctx.PlayerId, ct).ConfigureAwait(
            false
        );

        if (!ModerationPolicy.IsAllowed(permissions, ModerationAction.Kick))
        {
            await events
                .PublishAsync(
                    new ModerationActionDeniedEvent(ctx.PlayerId, message.UserId, ctx.RoomId, nameof(ModerationAction.Kick)),
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        var roomGrain = grainFactory.GetRoomGrain(ctx.RoomId);
        await roomGrain.KickUserAsync(actorCtx, message.UserId, ct).ConfigureAwait(false);
    }
}
