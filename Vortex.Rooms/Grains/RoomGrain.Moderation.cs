using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Players;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<bool> KickUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    )
    {
        if (
            actorCtx.PlayerId <= 0
            || actorCtx.RoomId != _state.RoomId
            || actorCtx.PlayerId == targetPlayerId
        )
        {
            return Task.FromResult(false);
        }

        return KickUserInternalAsync(actorCtx, targetPlayerId, ct);
    }

    /// <summary>Kicks a user without a human actor — for wired / system-driven kicks (the
    /// <c>wf_act_kick_user</c> action). Called directly on the grain from inside its own turn, so it is
    /// not a re-entrant grain-reference call.</summary>
    public Task<bool> KickUserFromWiredAsync(PlayerId targetPlayerId, CancellationToken ct) =>
        KickUserInternalAsync(ActionContext.CreateForWired(_state.RoomId), targetPlayerId, ct);

    private async Task<bool> KickUserInternalAsync(
        ActionContext ctx,
        PlayerId targetPlayerId,
        CancellationToken ct
    )
    {
        try
        {
            if (targetPlayerId <= 0 || !_state.AvatarsByPlayerId.ContainsKey(targetPlayerId))
            {
                return false;
            }

            await AvatarModule
                .RemoveAvatarFromPlayerAsync(ctx, targetPlayerId, ct)
                .ConfigureAwait(true);

            await _events
                .PublishAsync(
                    new PlayerKickedFromRoomEvent(
                        ctx.PlayerId,
                        targetPlayerId,
                        _state.RoomId.Value
                    ),
                    ct
                )
                .ConfigureAwait(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to kick player {TargetPlayerId} from room {RoomId}.",
                targetPlayerId,
                _state.RoomId
            );

            return false;
        }
    }

    public async Task<bool> MuteUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        int durationSeconds,
        CancellationToken ct
    )
    {
        if (actorCtx.PlayerId <= 0 || targetPlayerId <= 0 || actorCtx.RoomId != _state.RoomId)
        {
            return false;
        }

        if (durationSeconds <= 0 || actorCtx.PlayerId == targetPlayerId)
        {
            return false;
        }

        DateTime expiresUtc = DateTime.UtcNow.AddSeconds(durationSeconds);

        try
        {
            await _moderationStore.MuteAsync(_state.RoomId.Value, targetPlayerId, expiresUtc, ct);
            _state.MuteExpiresUtc[targetPlayerId] = expiresUtc;

            await _events
                .PublishAsync(
                    new PlayerMutedInRoomEvent(
                        actorCtx.PlayerId,
                        targetPlayerId,
                        _state.RoomId.Value,
                        durationSeconds
                    ),
                    ct
                )
                .ConfigureAwait(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to mute player {TargetPlayerId} in room {RoomId}.",
                targetPlayerId,
                _state.RoomId
            );
        }

        return false;
    }

    public async Task<bool> BanUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        int durationSeconds,
        CancellationToken ct
    )
    {
        try
        {
            if (actorCtx.PlayerId <= 0 || targetPlayerId <= 0 || actorCtx.RoomId != _state.RoomId)
            {
                return false;
            }

            if (durationSeconds <= 0 || actorCtx.PlayerId == targetPlayerId)
            {
                return false;
            }

            DateTime expiresUtc = DateTime.UtcNow.AddSeconds(durationSeconds);

            await _moderationStore.BanAsync(_state.RoomId.Value, targetPlayerId, expiresUtc, ct);
            await AvatarModule
                .RemoveAvatarFromPlayerAsync(actorCtx, targetPlayerId, ct)
                .ConfigureAwait(true);

            await _events
                .PublishAsync(
                    new PlayerBannedInRoomEvent(
                        actorCtx.PlayerId,
                        targetPlayerId,
                        _state.RoomId.Value,
                        durationSeconds
                    ),
                    ct
                )
                .ConfigureAwait(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to ban player {TargetPlayerId} in room {RoomId}.",
                targetPlayerId,
                _state.RoomId
            );

            return false;
        }
    }

    public async Task<bool> UnmuteUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    )
    {
        try
        {
            if (actorCtx.PlayerId <= 0 || targetPlayerId <= 0 || actorCtx.RoomId != _state.RoomId)
            {
                return false;
            }

            await _moderationStore
                .UnmuteAsync(_state.RoomId.Value, targetPlayerId, ct)
                .ConfigureAwait(true);
            _state.MuteExpiresUtc.Remove(targetPlayerId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to unmute player {TargetPlayerId} in room {RoomId}.",
                targetPlayerId,
                _state.RoomId
            );

            return false;
        }
    }

    public async Task<bool> UnbanUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    )
    {
        try
        {
            if (actorCtx.PlayerId <= 0 || targetPlayerId <= 0 || actorCtx.RoomId != _state.RoomId)
            {
                return false;
            }

            await _moderationStore
                .UnbanAsync(_state.RoomId.Value, targetPlayerId, ct)
                .ConfigureAwait(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to unban player {TargetPlayerId} in room {RoomId}.",
                targetPlayerId,
                _state.RoomId
            );

            return false;
        }
    }
}
