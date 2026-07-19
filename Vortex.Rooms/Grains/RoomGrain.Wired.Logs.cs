using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Wired;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public async Task<WiredRoomLogsComposer> GetWiredRoomLogsPageAsync(
        int page,
        int pageSize,
        int logLevelFilter,
        int logSourceFilter,
        string query,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        IQueryable<RoomWiredLogEntity> logs = dbCtx
            .RoomWiredLogs.AsNoTracking()
            .Where(l => l.RoomEntityId == _state.RoomId.Value);

        WiredLogLevel? logLevel = Enum.IsDefined(typeof(WiredLogLevel), logLevelFilter)
            ? (WiredLogLevel)logLevelFilter
            : null;

        if (logLevel.HasValue)
        {
            logs = logs.Where(l => l.LogLevel == logLevel.Value);
        }

        WiredLogSource? logSource = Enum.IsDefined(typeof(WiredLogSource), logSourceFilter)
            ? (WiredLogSource)logSourceFilter
            : null;

        if (logSource.HasValue)
        {
            logs = logs.Where(l => l.LogSource == logSource.Value);
        }

        string? trimmedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

        if (trimmedQuery is not null)
        {
            logs = logs.Where(l => EF.Functions.Like(l.Message, $"%{trimmedQuery}%"));
        }

        int totalEntries = await logs.CountAsync(ct);

        int safePageSize = pageSize > 0 ? pageSize : 25;
        int safePage = page > 0 ? page : 1;

        List<RoomWiredLogEntity> rows = await logs.OrderByDescending(l => l.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(ct);

        List<WiredRoomLogEntry> entries = rows.Select(row => new WiredRoomLogEntry
            {
                Id = row.Id,
                LogLevel = row.LogLevel,
                LogSource = row.LogSource,
                Message = row.Message,
                Timestamp = new DateTimeOffset(
                    row.CreatedAt,
                    TimeSpan.Zero
                ).ToUnixTimeMilliseconds(),
                TimestampStr = row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            })
            .ToList();

        return new WiredRoomLogsComposer
        {
            TotalEntries = totalEntries,
            CurrentPage = safePage,
            Amount = entries.Count,
            Entries = entries,
            LogLevelFilter = logLevel,
            LogSourceFilter = logSource,
            Query = trimmedQuery,
        };
    }
}
