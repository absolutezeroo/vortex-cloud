using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Incoming.Room.Action;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;

namespace Turbo.PacketHandlers.Room.Action;

public class UnmuteUserMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<UnmuteUserMessage>
{
    public async ValueTask HandleAsync(UnmuteUserMessage message, MessageContext ctx, CancellationToken ct)
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.UserId <= 0)
            return;

        var actorCtx = ctx.AsActionContext();
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
                        ctx.RoomId,
                        nameof(ModerationAction.Mute)
                    ),
                    ct
                )
                .ConfigureAwait(false);
            return;
        }

        var roomGrain = grainFactory.GetRoomGrain(ctx.RoomId);
        await roomGrain
            .UnmuteUserAsync(actorCtx, message.UserId, ct)
            .ConfigureAwait(false);
    }
}
