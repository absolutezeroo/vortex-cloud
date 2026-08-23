using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Room;

namespace Vortex.Dashboard.API.Api;

internal sealed partial class DashboardApiService
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
    public Task<object> ChatlogsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                (DateTime since, DateTime until) = ResolveWindow(
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

                int limit = ParseLimit(query["limit"], 100, 500);
                int page = ParsePage(query["page"]);
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

                var rows = await q.OrderByDescending(c => c.CreatedAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(c => new
                    {
                        c.Id,
                        c.CreatedAt,
                        roomId = c.RoomEntityId,
                        roomName = c.RoomEntity != null ? c.RoomEntity.Name : null,
                        playerId = c.PlayerEntityId,
                        playerName = c.PlayerEntity != null ? c.PlayerEntity.Name : null,
                        targetPlayerId = c.TargetPlayerEntityId,
                        targetPlayerName = c.TargetPlayerEntity != null
                            ? c.TargetPlayerEntity.Name
                            : null,
                        c.Message,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new
                {
                    count = rows.Count,
                    page,
                    limit,
                    total,
                    offset,
                    window = new { since, until },
                    filters = new
                    {
                        q = text,
                        player = playerId,
                        room = roomId,
                    },
                    items = rows,
                };
            },
            ct
        );
}
