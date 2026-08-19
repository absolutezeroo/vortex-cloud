using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Moderator;

/// <summary>
/// The room tool's "Send caution" / "Send message" buttons: one line to everybody in the room the
/// moderator has open. Distinct from <c>ModAlert</c>, which is aimed at a single named player.
/// </summary>
public class ModToolRoomAlertMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IEventPublisher events
) : IMessageHandler<ModToolRoomAlertMessage>
{
    public async ValueTask HandleAsync(
        ModToolRoomAlertMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrWhiteSpace(message.Message))
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        // Reaches every occupant of a room the actor need not be in, so it is gated on the same
        // hotel-wide capability as the rest of the room tool, not on any in-room controller level.
        if (!ModerationPolicy.IsAllowed(permissions, ModerationAction.Alert))
        {
            return;
        }

        int inspectedRoomId = await grainFactory
            .GetModerationQueueGrain()
            .GetInspectedRoomAsync(PlayerId.Parse(ctx.PlayerId))
            .ConfigureAwait(false);

        if (inspectedRoomId <= 0)
        {
            // No room tool was opened this session, so there is nothing this line could be aimed
            // at. Dropped rather than guessed at: the actor's own room is not the target — the tool
            // is routinely used on rooms the moderator is nowhere near.
            return;
        }

        RoomId roomId = inspectedRoomId;

        IComposer composer = message.IsCaution
            ? new ModeratorCautionEventMessageComposer { Message = message.Message }
            : new ModeratorMessageComposer { Message = message.Message };

        await grainFactory
            .GetRoomCore(roomId)
            .SendComposerToRoomAsync(composer)
            .ConfigureAwait(false);

        await events
            .PublishAsync(
                new RoomAlertedByStaffEvent(
                    ctx.PlayerId,
                    inspectedRoomId,
                    message.IsCaution,
                    message.Message
                ),
                ct
            )
            .ConfigureAwait(false);
    }
}
