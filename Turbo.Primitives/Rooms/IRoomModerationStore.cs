using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Turbo.Primitives.Rooms;

/// <summary>A persisted room mute that is still in effect.</summary>
public readonly record struct RoomMuteRecord(int PlayerId, DateTime ExpiresUtc);

/// <summary>
/// Persistence boundary for room-scoped bans and mutes (the <c>room_bans</c> / <c>room_mutes</c> tables).
/// Keeps moderation database access out of packet handlers and the chat hot path: the room grain loads
/// active mutes once on activation and enforces them in memory, while bans are checked at room entry.
/// </summary>
public interface IRoomModerationStore
{
    Task<bool> IsBannedAsync(int roomId, int playerId, CancellationToken ct = default);

    Task BanAsync(int roomId, int playerId, DateTime expiresUtc, CancellationToken ct = default);

    Task UnbanAsync(int roomId, int playerId, CancellationToken ct = default);

    Task MuteAsync(int roomId, int playerId, DateTime expiresUtc, CancellationToken ct = default);

    Task UnmuteAsync(int roomId, int playerId, CancellationToken ct = default);

    Task<IReadOnlyList<RoomMuteRecord>> GetActiveMutesAsync(
        int roomId,
        CancellationToken ct = default
    );
}
