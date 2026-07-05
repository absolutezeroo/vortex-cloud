using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Events;
using Turbo.Primitives.Messages.Outgoing.Moderation;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Moderator;

/// <summary>
/// Shared plumbing for the staff CFH-tool actions (ModKick/ModMute/ModAlert/...): unlike the
/// in-room moderation panel, the acting staff member is not necessarily standing in the target's
/// room, so the target room has to be resolved from their presence grain rather than
/// <c>ctx.RoomId</c>.
/// </summary>
internal static class ModToolActionHelper
{
    public static async Task<RoomId> GetTargetRoomIdAsync(
        IGrainFactory grainFactory,
        int targetUserId
    )
    {
        RoomPointerSnapshot activeRoom = await grainFactory
            .GetPlayerPresenceGrain(targetUserId)
            .GetActiveRoomAsync()
            .ConfigureAwait(false);

        return activeRoom.RoomId;
    }

    public static async Task<bool> IsAuthorizedAsync(
        IPermissionService permissionService,
        IEventPublisher events,
        int actorPlayerId,
        int targetPlayerId,
        RoomId roomId,
        ModerationAction action,
        CancellationToken ct
    )
    {
        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(actorPlayerId, ct)
            .ConfigureAwait(false);

        if (ModerationPolicy.IsAllowed(permissions, action))
        {
            return true;
        }

        await events
            .PublishAsync(
                new ModerationActionDeniedEvent(
                    actorPlayerId,
                    targetPlayerId,
                    roomId,
                    action.ToString()
                ),
                ct
            )
            .ConfigureAwait(false);

        return false;
    }

    public static async Task SendCautionIfPresentAsync(
        IGrainFactory grainFactory,
        int targetUserId,
        string message
    )
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        await grainFactory
            .GetPlayerPresenceGrain(targetUserId)
            .SendComposerAsync(new ModeratorCautionEventMessageComposer { Message = message })
            .ConfigureAwait(false);
    }

    public static async Task SendResultAsync(
        IGrainFactory grainFactory,
        int actorPlayerId,
        int targetUserId,
        bool success
    )
    {
        await grainFactory
            .GetPlayerPresenceGrain(actorPlayerId)
            .SendComposerAsync(
                new ModeratorActionResultMessageComposer
                {
                    UserId = targetUserId,
                    Success = success,
                }
            )
            .ConfigureAwait(false);
    }
}
