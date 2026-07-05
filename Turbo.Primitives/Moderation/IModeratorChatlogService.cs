using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Turbo.Primitives.Moderation;

public interface IModeratorChatlogService
{
    /// <summary>Most recent <paramref name="limit"/> chat lines (public and whispered — staff need
    /// full visibility) in a room, oldest first.</summary>
    Task<ChatlogBlockSnapshot> GetRoomChatlogAsync(
        int roomId,
        int limit,
        CancellationToken ct = default
    );

    /// <summary>A user's recent chat grouped by room, most-recently-active room first, each room's
    /// records oldest first.</summary>
    Task<ImmutableArray<ChatlogBlockSnapshot>> GetUserChatlogAsync(
        int userId,
        int roomLimit,
        int messagesPerRoom,
        CancellationToken ct = default
    );
}
