using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Rooms;

public interface IRoomAdvertisementService
{
    Task CreateAsync(
        int roomId,
        string name,
        string? description,
        int categoryId,
        bool extended,
        DateTime expiresAt,
        CancellationToken ct = default
    );

    /// <summary>Edits the ad copy of a currently-active advertisement. Returns null if the
    /// advertisement doesn't exist, has already expired, or isn't owned by actorPlayerId's room.</summary>
    Task<RoomAdvertisementSnapshot?> EditAsync(
        int advertisementId,
        int actorPlayerId,
        string name,
        string? description,
        CancellationToken ct = default
    );

    /// <summary>Expires an advertisement immediately (kept as a history row, not deleted -- see
    /// RoomAdvertisementEntity). Returns the owning RoomId on success, null otherwise.</summary>
    Task<RoomId?> CancelAsync(
        int advertisementId,
        int actorPlayerId,
        CancellationToken ct = default
    );

    /// <summary>Whether a room currently has a non-expired advertisement.</summary>
    Task<bool> HasActiveAdvertisementAsync(int roomId, CancellationToken ct = default);
}
