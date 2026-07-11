using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms;

namespace Turbo.Rooms.Grains;

public sealed partial class RoomGrain
{
    public async Task<bool> RegisterDoorbellRingAsync(PlayerId ringerId, CancellationToken ct)
    {
        if (ringerId <= 0)
        {
            return false;
        }

        if (_state.PendingDoorbellRingersMs.ContainsKey(ringerId))
        {
            return false;
        }

        _state.PendingDoorbellRingersMs[ringerId] = NowMs();

        try
        {
            IPlayerDirectoryGrain directory = _grainFactory.GetPlayerDirectoryGrain();
            string ringerName = await directory
                .GetPlayerNameAsync(ringerId, ct)
                .ConfigureAwait(true);
            ImmutableArray<PlayerId> notifyTargets = await GetPresentRightsHoldersAsync()
                .ConfigureAwait(true);

            await _grainFactory
                .GetPlayerPresenceGrain(ringerId)
                .SendComposerAsync(new DoorbellMessageComposer { Username = string.Empty })
                .ConfigureAwait(true);

            await Task.WhenAll(
                    notifyTargets.Select(id =>
                        _grainFactory
                            .GetPlayerPresenceGrain(id)
                            .SendComposerAsync(
                                new DoorbellMessageComposer { Username = ringerName }
                            )
                    )
                )
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to notify doorbell ring for player {RingerId} in room {RoomId}.",
                ringerId,
                _state.RoomId
            );
        }

        return true;
    }

    public Task<ImmutableArray<PlayerId>> GetPresentRightsHoldersAsync()
    {
        HashSet<PlayerId> targets = [.. _state.PlayerIdsWithRights];

        if (_state.RoomSnapshot.OwnerId > 0)
        {
            targets.Add(_state.RoomSnapshot.OwnerId);
        }

        targets.IntersectWith(_state.AvatarsByPlayerId.Keys);

        return Task.FromResult(targets.ToImmutableArray());
    }

    public Task<bool> TryRemoveDoorbellRingAsync(PlayerId ringerId, CancellationToken ct) =>
        Task.FromResult(_state.PendingDoorbellRingersMs.Remove(ringerId));

    /// <summary>Tick-driven sweep for rings nobody answered in time. Stays self-contained (no
    /// <c>_grainFactory.GetRoomGrain(_state.RoomId)</c> calls back to this same activation) since
    /// it runs from inside the room's own grain timer — a self-referential grain call from there
    /// would deadlock a non-reentrant grain.</summary>
    internal async Task ProcessDoorbellTimeoutsAsync(long nowMs, CancellationToken ct)
    {
        if (_state.PendingDoorbellRingersMs.Count == 0)
        {
            return;
        }

        List<PlayerId>? expired = null;

        foreach ((PlayerId ringerId, long startedMs) in _state.PendingDoorbellRingersMs)
        {
            if (nowMs - startedMs < _roomConfig.DoorbellTimeoutMs)
            {
                continue;
            }

            (expired ??= []).Add(ringerId);
        }

        if (expired is null)
        {
            return;
        }

        foreach (PlayerId ringerId in expired)
        {
            _state.PendingDoorbellRingersMs.Remove(ringerId);
        }

        try
        {
            IPlayerDirectoryGrain directory = _grainFactory.GetPlayerDirectoryGrain();
            ImmutableArray<PlayerId> notifyTargets = await GetPresentRightsHoldersAsync()
                .ConfigureAwait(true);

            foreach (PlayerId ringerId in expired)
            {
                string ringerName = await directory
                    .GetPlayerNameAsync(ringerId, ct)
                    .ConfigureAwait(true);

                await _grainFactory
                    .GetPlayerPresenceGrain(ringerId)
                    .SendComposerAsync(
                        new FlatAccessDeniedMessageComposer
                        {
                            RoomId = _state.RoomId,
                            Username = string.Empty,
                        }
                    )
                    .ConfigureAwait(true);
                await _grainFactory
                    .GetPlayerPresenceGrain(ringerId)
                    .SetPendingRoomAsync(RoomId.Invalid, false)
                    .ConfigureAwait(true);

                await Task.WhenAll(
                        notifyTargets.Select(id =>
                            _grainFactory
                                .GetPlayerPresenceGrain(id)
                                .SendComposerAsync(
                                    new FlatAccessDeniedMessageComposer
                                    {
                                        RoomId = _state.RoomId,
                                        Username = ringerName,
                                    }
                                )
                        )
                    )
                    .ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to process doorbell timeout sweep for room {RoomId}.",
                _state.RoomId
            );
        }
    }
}
