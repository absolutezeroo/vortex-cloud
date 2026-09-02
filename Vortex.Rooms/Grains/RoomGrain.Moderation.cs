using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Action;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<bool> KickUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    ) => ModerationSystem.KickUserAsync(actorCtx, targetPlayerId, ct);

    /// <summary>Kicks a user without a human actor — for wired / system-driven kicks (the
    /// <c>wf_act_kick_user</c> action). Called directly on the grain from inside its own turn, so it is
    /// not a re-entrant grain-reference call.</summary>
    public Task<bool> KickUserFromWiredAsync(PlayerId targetPlayerId, CancellationToken ct) =>
        ModerationSystem.KickUserFromWiredAsync(targetPlayerId, ct);

    public Task<bool> MuteUserFromWiredAsync(
        PlayerId targetPlayerId,
        int durationSeconds,
        CancellationToken ct
    ) => ModerationSystem.MuteUserFromWiredAsync(targetPlayerId, durationSeconds, ct);

    public Task<bool> MuteUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        int durationSeconds,
        CancellationToken ct
    ) => ModerationSystem.MuteUserAsync(actorCtx, targetPlayerId, durationSeconds, ct);

    public Task<bool> BanUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        int durationSeconds,
        CancellationToken ct
    ) => ModerationSystem.BanUserAsync(actorCtx, targetPlayerId, durationSeconds, ct);

    public Task SetHotelMuteAsync(PlayerId targetPlayerId, DateTime? expiresUtc) =>
        ModerationSystem.SetHotelMuteAsync(targetPlayerId, expiresUtc);

    public Task<bool> UnmuteUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    ) => ModerationSystem.UnmuteUserAsync(actorCtx, targetPlayerId, ct);

    public Task<bool> UnbanUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    ) => ModerationSystem.UnbanUserAsync(actorCtx, targetPlayerId, ct);

    public async Task<bool> ApplyStaffRoomActionsAsync(
        PlayerId actorPlayerId,
        bool unlockDoor,
        bool resetNameAndDescription,
        bool kickUsers,
        CancellationToken ct
    )
    {
        // The room tool reaches into a room its user neither owns nor stands in, so no controller
        // level can gate it -- which left the packet handler as the only check, on a method of a
        // public grain interface (ROOMG-GATE-038). The grain is the boundary.
        if (
            !await SecurityModule
                .HasCapabilityAsync(actorPlayerId, Capabilities.Room.ModerateAny)
                .ConfigureAwait(true)
        )
        {
            _logger.LogWarning(
                "Player {ActorPlayerId} tried to apply staff room actions to room {RoomId} without "
                    + "{Capability}.",
                actorPlayerId,
                _state.RoomId,
                Capabilities.Room.ModerateAny
            );

            return false;
        }

        return await ModerationSystem
            .ApplyStaffRoomActionsAsync(
                actorPlayerId,
                unlockDoor,
                resetNameAndDescription,
                kickUsers,
                ct
            )
            .ConfigureAwait(true);
    }
}
