using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Dashboard.API.Api.Safety.Contracts;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;

namespace Vortex.Dashboard.API.Api.Safety;

internal sealed class ChatlogReads(IDbContextFactory<VortexDbContext> dbContextFactory)
    : DashboardReads(dbContextFactory)
{
    /// <summary>
    /// Chat search across rooms. The room forensics timeline and the player profile already show
    /// chat *in context* -- what neither can answer is "who said this word, anywhere", which is the
    /// question an operator arrives with when the report names a phrase and not a room.
    ///
    /// <para>
    /// At least one narrowing filter is required. The table carries composite indexes on
    /// (room, created_at) and (player, created_at), so a search that gives neither a room, a player
    /// nor a text fragment scans the whole window -- in the emulator's own process. That is a 400,
    /// not a slow page.
    /// </para>
    /// </summary>
    public Task<ChatlogPage> ChatlogsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<ChatlogPage>(
            async db =>
            {
                (DateTime since, DateTime until) = TimeWindow.Resolve(
                    query,
                    DateTime.UtcNow,
                    TimeSpan.FromDays(7)
                );

                string? text = string.IsNullOrWhiteSpace(query["q"]) ? null : query["q"]!.Trim();
                int? playerId = int.TryParse(query["player"], out int player) ? player : null;
                int? roomId = int.TryParse(query["room"], out int room) ? room : null;

                if (text is null && playerId is null && roomId is null)
                {
                    throw new DashboardQueryException(
                        "filter_required",
                        "A chatlog search needs a room, a player or a text fragment to narrow it."
                    );
                }

                int limit = QueryValues.Limit(query["limit"], 100, 500);
                int page = QueryValues.Page(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);

                IQueryable<RoomChatlogEntity> q = db
                    .Chatlogs.AsNoTracking()
                    .Where(c => c.CreatedAt >= since && c.CreatedAt <= until);

                if (roomId is not null)
                {
                    q = q.Where(c => c.RoomEntityId == roomId.Value);
                }

                if (playerId is not null)
                {
                    q = q.Where(c =>
                        c.PlayerEntityId == playerId.Value
                        || c.TargetPlayerEntityId == playerId.Value
                    );
                }

                if (text is not null)
                {
                    // ponytail: leading-wildcard LIKE, so the text filter cannot use an index and
                    // leans on the room/player filter and the window to stay cheap. If chat search
                    // becomes a daily tool, a FULLTEXT index on room_chatlogs.message is the upgrade.
                    q = q.Where(c => EF.Functions.Like(c.Message, $"%{text}%"));
                }

                int total = await q.CountAsync(ct).ConfigureAwait(false);

                List<ChatlogEntry> rows = await q.OrderByDescending(c => c.CreatedAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(c => new ChatlogEntry(
                        c.Id,
                        c.CreatedAt,
                        c.RoomEntityId,
                        c.RoomEntity != null ? c.RoomEntity.Name : null,
                        c.PlayerEntityId,
                        c.PlayerEntity != null ? c.PlayerEntity.Name : null,
                        c.TargetPlayerEntityId,
                        c.TargetPlayerEntity != null ? c.TargetPlayerEntity.Name : null,
                        c.Message
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new ChatlogPage(
                    rows.Count,
                    page,
                    limit,
                    total,
                    offset,
                    new ChatlogWindow(since, until),
                    new ChatlogFilters(text, playerId, roomId),
                    rows
                );
            },
            ct
        );
}
