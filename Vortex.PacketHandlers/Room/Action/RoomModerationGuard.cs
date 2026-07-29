using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Room.Action;

/// <summary>
/// Shared authorization for the in-room moderation packets (kick / mute / ban and their undos).
/// </summary>
/// <remarks>
/// These are room-scoped actions, not staff actions: in Habbo the owner always moderates their own
/// room, rights-holders and guild members do so when the room's mod settings allow it, and staff
/// moderate anywhere. That room-scoped authority is resolved inside <c>RoomGrain</c>, where the
/// controller level and the room's <c>WhoCanMute/Kick/Ban</c> settings live.
/// <para>
/// So the only thing left to check here is the relative-rank guard. Gating these packets on the
/// staff capability instead — which is what they used to do — meant the default "player" role, which
/// holds no capabilities at all, could never kick anyone: room owners could not moderate their own
/// rooms, and the room's mod settings were dead configuration.
/// </para>
/// </remarks>
internal static class RoomModerationGuard
{
    /// <summary>
    /// Resolves both subjects and applies the relative-rank guard, auditing a denial. Returns false
    /// when the actor outranks nothing — i.e. the target is higher-ranked staff.
    /// </summary>
    public static async ValueTask<bool> CanActOnTargetAsync(
        IPermissionService permissionService,
        IEventPublisher events,
        MessageContext ctx,
        RoomId roomId,
        int targetUserId,
        ModerationAction action,
        CancellationToken ct
    )
    {
        PermissionSet actorPermissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);
        PermissionSet targetPermissions = await permissionService
            .ResolveForPlayerAsync(targetUserId, ct)
            .ConfigureAwait(false);

        if (ModerationPolicy.CanActOnTarget(actorPermissions, targetPermissions))
        {
            return true;
        }

        await AuditDenialAsync(events, ctx, roomId, targetUserId, action, ct).ConfigureAwait(false);

        return false;
    }

    /// <summary>
    /// Audits a denial that came from the room itself (the mod settings did not grant the actor
    /// authority here), so both refusal paths leave the same trail.
    /// </summary>
    public static ValueTask AuditDenialAsync(
        IEventPublisher events,
        MessageContext ctx,
        RoomId roomId,
        int targetUserId,
        ModerationAction action,
        CancellationToken ct
    )
    {
        return new ValueTask(
            events.PublishAsync(
                new ModerationActionDeniedEvent(
                    ctx.PlayerId,
                    targetUserId,
                    roomId,
                    action.ToString()
                ),
                ct
            )
        );
    }
}
