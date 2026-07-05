using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Primitives.Moderation;

namespace Turbo.Rooms;

/// <summary>
/// Reads <c>room_chatlogs</c> for the staff mod tool's chatlog views. Read-only, low call volume
/// (a staff member manually requesting a log) — no caching, straightforward per-room queries.
/// </summary>
internal sealed class ModeratorChatlogService(IDbContextFactory<TurboDbContext> dbContextFactory)
    : IModeratorChatlogService
{
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;

    public async Task<ChatlogBlockSnapshot> GetRoomChatlogAsync(
        int roomId,
        int limit,
        CancellationToken ct = default
    )
    {
        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        string roomName =
            await dbCtx
                .Rooms.AsNoTracking()
                .Where(r => r.Id == roomId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false)
            ?? string.Empty;

        List<ChatlogRecordSnapshot> records = await dbCtx
            .Chatlogs.AsNoTracking()
            .Where(c => c.RoomEntityId == roomId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .Select(c => new ChatlogRecordSnapshot
            {
                TimeStampUtc = c.CreatedAt,
                ChatterId = c.PlayerEntityId,
                ChatterName = c.PlayerEntity != null ? c.PlayerEntity.Name : string.Empty,
                Message = c.Message,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        records.Reverse();

        return new ChatlogBlockSnapshot
        {
            RoomId = roomId,
            RoomName = roomName,
            Records = records.ToImmutableArray(),
        };
    }

    public async Task<ImmutableArray<ChatlogBlockSnapshot>> GetUserChatlogAsync(
        int userId,
        int roomLimit,
        int messagesPerRoom,
        CancellationToken ct = default
    )
    {
        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<int> recentRoomIds = await dbCtx
            .Chatlogs.AsNoTracking()
            .Where(c => c.PlayerEntityId == userId)
            .GroupBy(c => c.RoomEntityId)
            .OrderByDescending(g => g.Max(c => c.CreatedAt))
            .Take(roomLimit)
            .Select(g => g.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        ImmutableArray<ChatlogBlockSnapshot>.Builder blocks =
            ImmutableArray.CreateBuilder<ChatlogBlockSnapshot>(recentRoomIds.Count);

        foreach (int roomId in recentRoomIds)
        {
            string roomName =
                await dbCtx
                    .Rooms.AsNoTracking()
                    .Where(r => r.Id == roomId)
                    .Select(r => r.Name)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false)
                ?? string.Empty;

            List<ChatlogRecordSnapshot> records = await dbCtx
                .Chatlogs.AsNoTracking()
                .Where(c => c.RoomEntityId == roomId && c.PlayerEntityId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(messagesPerRoom)
                .Select(c => new ChatlogRecordSnapshot
                {
                    TimeStampUtc = c.CreatedAt,
                    ChatterId = c.PlayerEntityId,
                    ChatterName = c.PlayerEntity != null ? c.PlayerEntity.Name : string.Empty,
                    Message = c.Message,
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            records.Reverse();

            blocks.Add(
                new ChatlogBlockSnapshot
                {
                    RoomId = roomId,
                    RoomName = roomName,
                    Records = records.ToImmutableArray(),
                }
            );
        }

        return blocks.MoveToImmutable();
    }
}
