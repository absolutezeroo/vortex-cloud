using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Turbo.Primitives.Action;
using Turbo.Primitives.Events;
using Turbo.Primitives.Players;

namespace Turbo.Rooms.Grains;

public sealed partial class RoomGrain
{
    public async Task<bool> KickUserAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        CancellationToken ct
    )
    {
        try
        {
            if (actorCtx.PlayerId <= 0 || targetPlayerId <= 0 || actorCtx.RoomId != _state.RoomId)
                return false;

            if (actorCtx.PlayerId == targetPlayerId)
                return false;

            if (!_state.AvatarsByPlayerId.ContainsKey(targetPlayerId))
                return false;

            await AvatarModule.RemoveAvatarFromPlayerAsync(actorCtx, targetPlayerId, ct).ConfigureAwait(
                false
            );

            await _events
                .PublishAsync(
                    new PlayerKickedFromRoomEvent(
                        actorCtx.PlayerId,
                        targetPlayerId,
                        _state.RoomId.Value
                    ),
                    ct
                )
                .ConfigureAwait(false);

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
            return false;

        if (durationSeconds <= 0 || actorCtx.PlayerId == targetPlayerId)
            return false;

        var expiresUtc = DateTime.UtcNow.AddSeconds(durationSeconds);

        try
        {
            _state.MuteExpiresUtc[targetPlayerId] = expiresUtc;
            await _moderationStore.MuteAsync(_state.RoomId.Value, targetPlayerId, expiresUtc, ct);

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
                .ConfigureAwait(false);

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
                return false;

            if (durationSeconds <= 0 || actorCtx.PlayerId == targetPlayerId)
                return false;

            var expiresUtc = DateTime.UtcNow.AddSeconds(durationSeconds);

            await _moderationStore.BanAsync(_state.RoomId.Value, targetPlayerId, expiresUtc, ct);
            await AvatarModule.RemoveAvatarFromPlayerAsync(actorCtx, targetPlayerId, ct).ConfigureAwait(
                false
            );

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
                .ConfigureAwait(false);

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
                return false;

            _state.MuteExpiresUtc.Remove(targetPlayerId);
            await _moderationStore.UnmuteAsync(_state.RoomId.Value, targetPlayerId, ct).ConfigureAwait(
                false
            );

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
                return false;

            await _moderationStore.UnbanAsync(_state.RoomId.Value, targetPlayerId, ct).ConfigureAwait(
                false
            );

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
