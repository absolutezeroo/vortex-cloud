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
        // Room-scoped: the owner / rights-holders / guild members are authorized by the room's
        // own mod settings inside RoomGrain. All that is enforced here is that nobody sanctions
        // higher-ranked staff.
        if (
            !await RoomModerationGuard
                .CanActOnTargetAsync(
                    permissionService,
                    events,
                    ctx,
                    ctx.RoomId,
                    message.UserId,
                    ModerationAction.Kick,
                    ct
                )
                .ConfigureAwait(false)
        )
        {
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(ctx.RoomId);
        bool applied = await roomGrain
            .KickUserAsync(actorCtx, message.UserId, ct)
            .ConfigureAwait(false);

        if (!applied)
        {
            // The room's mod settings did not grant this actor authority here.
            await RoomModerationGuard
                .AuditDenialAsync(
                    events,
                    ctx,
                    ctx.RoomId,
                    message.UserId,
                    ModerationAction.Kick,
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
