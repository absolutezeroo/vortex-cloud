using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Moderation;

public interface IModeratorRoomVisitService
{
    /// <summary>Where a user has been, most recent visit first, read from the append-only
    /// <c>room_entry_logs</c>. Returns an empty array for a player id that does not exist.</summary>
    Task<RoomVisitHistorySnapshot> GetUserRoomVisitsAsync(
        int userId,
        int limit,
        CancellationToken ct = default
    );
}

/// <summary>The visit list plus the name the mod tool titles the window with.</summary>
public readonly record struct RoomVisitHistorySnapshot(
    int UserId,
    string UserName,
    ImmutableArray<RoomVisitSnapshot> Visits
);
