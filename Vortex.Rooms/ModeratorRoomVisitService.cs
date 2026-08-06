using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Primitives.Moderation;

namespace Vortex.Rooms;

/// <summary>
/// Reads <c>room_entry_logs</c> for the staff mod tool's room-visits view. Same shape as
/// <see cref="ModeratorChatlogService"/>: read-only, manually triggered by a staff member, so no
/// caching.
/// </summary>
internal sealed class ModeratorRoomVisitService(IDbContextFactory<VortexDbContext> dbContextFactory)
    : IModeratorRoomVisitService
{
    private readonly IDbContextFactory<VortexDbContext> _dbContextFactory = dbContextFactory;

    public async Task<RoomVisitHistorySnapshot> GetUserRoomVisitsAsync(
        int userId,
        int limit,
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        string userName =
            await dbCtx
                .Players.AsNoTracking()
                .Where(p => p.Id == userId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false)
            ?? string.Empty;

        // The client has no date field on a visit row — only an hour and a minute — so the entry
        // timestamp is projected down to local wall-clock parts here rather than on the wire.
        ImmutableArray<RoomVisitSnapshot> visits =
        [
            .. await dbCtx
                .RoomEntryLogs.AsNoTracking()
                .Where(v => v.PlayerEntityId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .Take(limit)
                .Select(v => new RoomVisitSnapshot
                {
                    RoomId = v.RoomEntityId,
                    RoomName = v.RoomEntity != null ? v.RoomEntity.Name : string.Empty,
                    EnterHour = v.CreatedAt.Hour,
                    EnterMinute = v.CreatedAt.Minute,
                })
                .ToListAsync(ct)
                .ConfigureAwait(false),
        ];

        return new RoomVisitHistorySnapshot(userId, userName, visits);
    }
}
