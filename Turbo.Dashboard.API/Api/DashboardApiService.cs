using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Database.Context;
using Turbo.Observability.Configuration;
using Turbo.Observability.Metrics;
using Turbo.Observability.Runtime;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.Dashboard.API.Api;

internal sealed class DashboardApiService(
    IDbContextFactory<TurboDbContext> dbContextFactory,
    IGrainFactory grainFactory,
    ISessionGateway sessionGateway,
    ILiveStatsAggregator liveStats,
    IIncidentDetectionService incidentDetection,
    IInfrastructureHealthService infrastructureHealth,
    ClubMetrics clubMetrics,
    IOptions<ObservabilityConfig> options
)
{
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ISessionGateway _sessionGateway = sessionGateway;
    private readonly ILiveStatsAggregator _liveStats = liveStats;
    private readonly IIncidentDetectionService _incidentDetection = incidentDetection;
    private readonly IInfrastructureHealthService _infrastructureHealth = infrastructureHealth;
    private readonly ClubMetrics _clubMetrics = clubMetrics;
    private readonly ObservabilityConfig _config = options.Value;

    private static readonly TimeSpan TotalsCacheTtl = TimeSpan.FromSeconds(30);
    private volatile CachedTotals? _cachedTotals;

    public async Task<object> PacketStatsAsync(CancellationToken ct)
    {
        var live = await _liveStats.GetSnapshotAsync().ConfigureAwait(false);

        return new
        {
            packetsPerSecond = Math.Round(live.PacketsPerSecond, 2),
            errorsPerMinute = Math.Round(live.ErrorsPerMinute, 2),
            latencyP50Ms = Math.Round(live.LatencyP50Ms, 2),
            latencyP95Ms = Math.Round(live.LatencyP95Ms, 2),
            topOperations = live.TopOperations.Select(o => new
            {
                operation = o.Operation,
                packetsPerMinute = Math.Round(o.PacketsPerMinute, 2),
            }),
            topFailedOperations = live.TopFailedOperations.Select(o => new
            {
                operation = o.Operation,
                packetsPerMinute = Math.Round(o.PacketsPerMinute, 2),
            }),
        };
    }

    public Task<InfrastructureHealthSnapshot> InfrastructureAsync(CancellationToken ct) =>
        _infrastructureHealth.GetStatusAsync(ct);

    public async Task<object> OverviewAsync(DateTime startedAtUtc, CancellationToken ct)
    {
        var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            var health = await _infrastructureHealth.GetStatusAsync(ct).ConfigureAwait(false);
            var incidents = await _incidentDetection.DetectAsync(ct).ConfigureAwait(false);
            var live = await _liveStats.GetSnapshotAsync().ConfigureAwait(false);
            var activeRooms = await _grainFactory
                .GetRoomDirectoryGrain()
                .GetActiveRoomsAsync()
                .ConfigureAwait(false);
            var since = DateTime.UtcNow.AddHours(-1);

            var byCategory = await db
                .AuditEvents.AsNoTracking()
                .Where(a => a.OccurredAt >= since)
                .GroupBy(a => a.Category)
                .Select(g => new { category = g.Key.ToString(), count = g.Count() })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var totals = await GetTotalsAsync(db, ct).ConfigureAwait(false);

            return new
            {
                status = health.Overall,
                health = health,
                uptimeSeconds = (long)(DateTime.UtcNow - startedAtUtc).TotalSeconds,
                managedMemoryMb = GC.GetTotalMemory(false) / 1024 / 1024,
                activeSessions = _sessionGateway.GetActiveSessionCount(),
                activeRooms = activeRooms.Length,
                activeClubSubscribers = _clubMetrics.ActiveSubscribers,
                incidents = incidents,
                live = new
                {
                    packetsPerSecond = Math.Round(live.PacketsPerSecond, 2),
                    errorsPerMinute = Math.Round(live.ErrorsPerMinute, 2),
                    latencyP50Ms = Math.Round(live.LatencyP50Ms, 2),
                    latencyP95Ms = Math.Round(live.LatencyP95Ms, 2),
                    topAbusers = live.TopAbusers.Select(a => new
                    {
                        playerId = a.PlayerId,
                        packetsPerMinute = a.PacketsPerMinute,
                    }),
                    topRooms = live.TopRooms.Select(r => new
                    {
                        roomId = r.RoomId,
                        packetsPerMinute = r.PacketsPerMinute,
                    }),
                },
                auditLastHourByCategory = byCategory,
                totals = new
                {
                    audit = totals.Audit,
                    ledger = totals.Ledger,
                    items = totals.Items,
                    performance = totals.Performance,
                    asOf = totals.AtUtc,
                },
            };
        }
        finally
        {
            await db.DisposeAsync().ConfigureAwait(false);
        }
    }

    public Task<IncidentDetectionSnapshot> IncidentsAsync(CancellationToken ct) =>
        _incidentDetection.DetectAsync(ct);

    public Task<object> AuditAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var limit = ParseLimit(query["limit"], 50, 500);
                var page = ParsePage(query["page"]);
                var offset = Math.Max(0, (page - 1) * limit);

                var q = db.AuditEvents.AsNoTracking();

                var since = ParseDateTime(query["since"]);
                var until = ParseDateTime(query["until"]);

                if (since is not null)
                    q = q.Where(a => a.OccurredAt >= since.Value);

                if (until is not null)
                    q = q.Where(a => a.OccurredAt <= until.Value);

                if (int.TryParse(query["actor"], out var actor))
                    q = q.Where(a => a.ActorPlayerId == actor);

                if (int.TryParse(query["target"], out var target))
                    q = q.Where(a => a.TargetPlayerId == target);

                if (Enum.TryParse<AuditCategory>(query["category"], ignoreCase: true, out var cat))
                    q = q.Where(a => a.Category == cat);

                var action = query["action"];
                if (!string.IsNullOrWhiteSpace(action))
                    q = q.Where(a => a.Action == action);

                var total = await q.CountAsync(ct).ConfigureAwait(false);

                var rows = await q.OrderByDescending(a => a.OccurredAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(a => new
                    {
                        a.Id,
                        a.OccurredAt,
                        category = a.Category.ToString(),
                        a.Action,
                        severity = a.Severity.ToString(),
                        result = a.Result.ToString(),
                        a.ActorPlayerId,
                        a.TargetPlayerId,
                        a.RoomId,
                        a.ItemId,
                        a.IpHash,
                        a.CorrelationId,
                        a.Data,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var playerIds = NormalizeIds(
                    rows.SelectMany(a => new[] { a.ActorPlayerId, a.TargetPlayerId })
                );

                var playerNames = await LoadPlayerNamesAsync(db, playerIds, ct)
                    .ConfigureAwait(false);

                var rowsWithNames = rows.Select(a => new
                    {
                        a.Id,
                        a.OccurredAt,
                        a.category,
                        a.Action,
                        a.severity,
                        a.result,
                        a.ActorPlayerId,
                        actorName = ResolvePlayerName(playerNames, a.ActorPlayerId),
                        a.TargetPlayerId,
                        targetName = ResolvePlayerName(playerNames, a.TargetPlayerId),
                        a.RoomId,
                        a.ItemId,
                        a.IpHash,
                        a.CorrelationId,
                        a.Data,
                    })
                    .ToList();

                return new
                {
                    count = rows.Count,
                    page,
                    limit,
                    total,
                    offset,
                    items = rowsWithNames,
                };
            },
            ct
        );

    public Task<object> EconomyAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var limit = ParseLimit(query["limit"], 50, 500);
                var page = ParsePage(query["page"]);
                var offset = Math.Max(0, (page - 1) * limit);
                var q = db.EconomyLedger.AsNoTracking();

                var since = ParseDateTime(query["since"]);
                var until = ParseDateTime(query["until"]);

                if (since is not null)
                    q = q.Where(l => l.OccurredAt >= since.Value);

                if (until is not null)
                    q = q.Where(l => l.OccurredAt <= until.Value);

                if (int.TryParse(query["player"], out var player))
                    q = q.Where(l => l.PlayerId == player);

                var total = await q.CountAsync(ct).ConfigureAwait(false);

                var rows = await q.OrderByDescending(l => l.OccurredAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(l => new
                    {
                        l.Id,
                        l.OccurredAt,
                        l.PlayerId,
                        l.Currency,
                        l.ActivityPointType,
                        l.Delta,
                        l.BalanceAfter,
                        reason = l.Reason.ToString(),
                        l.RefId,
                        l.CorrelationId,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var playerIds = NormalizeIds(rows.Select(l => (long?)l.PlayerId));

                var playerNames = await LoadPlayerNamesAsync(db, playerIds, ct)
                    .ConfigureAwait(false);

                var rowsWithNames = rows.Select(l => new
                    {
                        l.Id,
                        l.OccurredAt,
                        l.PlayerId,
                        playerName = ResolvePlayerName(playerNames, l.PlayerId),
                        l.Currency,
                        l.ActivityPointType,
                        l.Delta,
                        l.BalanceAfter,
                        l.reason,
                        l.RefId,
                        l.CorrelationId,
                    })
                    .ToList();

                return new
                {
                    count = rows.Count,
                    page,
                    limit,
                    total,
                    offset,
                    items = rowsWithNames,
                };
            },
            ct
        );

    public Task<object?> ItemAsync(string idText, NameValueCollection query, CancellationToken ct)
    {
        if (!long.TryParse(idText, out var itemId))
            return Task.FromResult<object?>(null);

        return QueryAsync<object?>(
            async db =>
            {
                var limit = ParseLimit(query["limit"], 50, 500);
                var page = ParsePage(query["page"]);
                var offset = Math.Max(0, (page - 1) * limit);
                var q = db.ItemEvents.AsNoTracking().Where(i => i.ItemId == itemId);

                var since = ParseDateTime(query["since"]);
                var until = ParseDateTime(query["until"]);

                if (since is not null)
                    q = q.Where(i => i.OccurredAt >= since.Value);

                if (until is not null)
                    q = q.Where(i => i.OccurredAt <= until.Value);

                var total = await q.CountAsync(ct).ConfigureAwait(false);

                var itemSnapshot = await db
                    .Furnitures.AsNoTracking()
                    .Where(f => f.Id == itemId)
                    .Select(f => new
                    {
                        f.Id,
                        definitionId = (int?)f.FurnitureDefinitionEntityId,
                        definitionName = f.FurnitureDefinitionEntity != null
                            ? f.FurnitureDefinitionEntity.Name
                            : null,
                        ownerPlayerId = (int?)f.PlayerEntityId,
                        ownerName = f.PlayerEntity != null ? f.PlayerEntity.Name : null,
                        roomId = (int?)f.RoomEntityId,
                        roomName = f.RoomEntity != null ? f.RoomEntity.Name : null,
                        roomX = (int?)f.X,
                        roomY = (int?)f.Y,
                        roomZ = f.Z,
                        f.ExtraData,
                        updatedAt = f.UpdatedAt,
                    })
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                var rows = await q.OrderBy(i => i.OccurredAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(i => new
                    {
                        i.Id,
                        i.OccurredAt,
                        eventType = i.EventType.ToString(),
                        i.ActorPlayerId,
                        i.FromOwnerId,
                        i.ToOwnerId,
                        i.RoomId,
                        i.CorrelationId,
                        i.Data,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var rowPlayerIds = NormalizeIds(
                    rows.SelectMany(r => new[] { r.ActorPlayerId, r.FromOwnerId, r.ToOwnerId })
                );

                var rowPlayerNames = await LoadPlayerNamesAsync(db, rowPlayerIds, ct)
                    .ConfigureAwait(false);

                var rowRoomIds = NormalizeIds(rows.Select(r => r.RoomId));

                var rowRoomNames = await LoadRoomNamesAsync(db, rowRoomIds, ct)
                    .ConfigureAwait(false);

                var rowsWithNames = rows.Select(r => new
                    {
                        r.Id,
                        r.OccurredAt,
                        r.eventType,
                        r.ActorPlayerId,
                        actorPlayerName = ResolvePlayerName(rowPlayerNames, r.ActorPlayerId),
                        r.FromOwnerId,
                        fromOwnerName = ResolvePlayerName(rowPlayerNames, r.FromOwnerId),
                        r.ToOwnerId,
                        toOwnerName = ResolvePlayerName(rowPlayerNames, r.ToOwnerId),
                        r.RoomId,
                        roomName = r.RoomId != null
                        && rowRoomNames.TryGetValue(r.RoomId.Value, out var roomName)
                            ? roomName
                            : null,
                        r.CorrelationId,
                        r.Data,
                    })
                    .ToList();

                return new
                {
                    itemId,
                    snapshot = itemSnapshot,
                    page,
                    limit,
                    total,
                    offset,
                    count = rows.Count,
                    history = rowsWithNames,
                };
            },
            ct
        );
    }

    public Task<object> SearchAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var term = (query["q"] ?? string.Empty).Trim();
                var limit = ParseLimit(query["limit"], 50, 500);
                var page = ParsePage(query["page"]);
                var offset = Math.Max(0, (page - 1) * limit);

                var since = ParseDateTime(query["since"]);
                var until = ParseDateTime(query["until"]);

                // Correlation id: 32 hex chars (Guid "N").
                if (term.Length == 32 && term.All(Uri.IsHexDigit))
                {
                    var audit = db.AuditEvents.AsNoTracking().Where(a => a.CorrelationId == term);

                    if (since is not null)
                        audit = audit.Where(a => a.OccurredAt >= since.Value);

                    if (until is not null)
                        audit = audit.Where(a => a.OccurredAt <= until.Value);

                    var ledger = db
                        .EconomyLedger.AsNoTracking()
                        .Where(l => l.CorrelationId == term);

                    if (since is not null)
                        ledger = ledger.Where(l => l.OccurredAt >= since.Value);

                    if (until is not null)
                        ledger = ledger.Where(l => l.OccurredAt <= until.Value);

                    var items = db.ItemEvents.AsNoTracking().Where(i => i.CorrelationId == term);

                    if (since is not null)
                        items = items.Where(i => i.OccurredAt >= since.Value);

                    if (until is not null)
                        items = items.Where(i => i.OccurredAt <= until.Value);

                    var auditRows = await audit
                        .OrderBy(a => a.OccurredAt)
                        .Skip(offset)
                        .Take(limit)
                        .Select(a => new
                        {
                            a.OccurredAt,
                            category = a.Category.ToString(),
                            a.Action,
                            a.ActorPlayerId,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var auditActorIds = NormalizeIds(auditRows.Select(a => a.ActorPlayerId));

                    var auditActorNames = await LoadPlayerNamesAsync(db, auditActorIds, ct)
                        .ConfigureAwait(false);

                    var auditRowsWithNames = auditRows
                        .Select(a => new
                        {
                            a.OccurredAt,
                            category = a.category,
                            a.Action,
                            a.ActorPlayerId,
                            actorName = ResolvePlayerName(auditActorNames, a.ActorPlayerId),
                        })
                        .ToList();

                    var ledgerRows = await ledger
                        .OrderBy(l => l.OccurredAt)
                        .Skip(offset)
                        .Take(limit)
                        .Select(l => new
                        {
                            l.OccurredAt,
                            l.PlayerId,
                            l.Currency,
                            l.Delta,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var itemRows = await items
                        .OrderBy(i => i.OccurredAt)
                        .Skip(offset)
                        .Take(limit)
                        .Select(i => new
                        {
                            i.OccurredAt,
                            i.ItemId,
                            eventType = i.EventType.ToString(),
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    return new
                    {
                        kind = "correlationId",
                        term,
                        page,
                        limit,
                        offset,
                        auditTotal = await audit.CountAsync(ct).ConfigureAwait(false),
                        ledgerTotal = await ledger.CountAsync(ct).ConfigureAwait(false),
                        itemTotal = await items.CountAsync(ct).ConfigureAwait(false),
                        audit = auditRowsWithNames,
                        ledger = ledgerRows,
                        items = itemRows,
                    };
                }

                if (int.TryParse(term, out var id))
                {
                    var playerIdLong = (long)id;
                    var profileWindowSince = since ?? DateTime.UtcNow.AddHours(-24);
                    var profileWindowUntil = until ?? DateTime.UtcNow;

                    var player = await db
                        .Players.AsNoTracking()
                        .Where(p => p.Id == id)
                        .Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.Motto,
                            p.Figure,
                            p.CreatedAt,
                            p.UpdatedAt,
                            status = p.PlayerStatus.ToString(),
                            gender = p.Gender.ToString(),
                            perks = p.PlayerPerks.ToString(),
                        })
                        .FirstOrDefaultAsync(ct)
                        .ConfigureAwait(false);

                    var playerCurrencies = await db
                        .PlayerCurrencies.AsNoTracking()
                        .Where(pc => pc.PlayerEntityId == id)
                        .Select(pc => new
                        {
                            pc.CurrencyTypeEntityId,
                            amount = pc.Amount,
                            currency = pc.CurrencyTypeEntity != null
                                ? pc.CurrencyTypeEntity.Name
                                    ?? pc.CurrencyTypeEntity.CurrencyType.ToString()
                                : pc.CurrencyTypeEntityId.ToString(),
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var ownedRooms = await db
                        .Rooms.AsNoTracking()
                        .Where(r => r.PlayerEntityId == id)
                        .OrderByDescending(r => r.LastActive)
                        .Take(8)
                        .Select(r => new
                        {
                            roomId = r.Id,
                            roomName = r.Name,
                            r.UsersNow,
                            r.PlayersMax,
                            r.LastActive,
                            model = r.RoomModelEntity.Name,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var ownedItems = await db
                        .Furnitures.AsNoTracking()
                        .Where(f => f.PlayerEntityId == id)
                        .OrderByDescending(f => f.UpdatedAt)
                        .Take(16)
                        .Select(f => new
                        {
                            itemId = (long)f.Id,
                            definitionId = (int?)f.FurnitureDefinitionEntityId,
                            definitionName = f.FurnitureDefinitionEntity != null
                                ? f.FurnitureDefinitionEntity.Name
                                : null,
                            f.RoomEntityId,
                            roomName = f.RoomEntity != null ? f.RoomEntity.Name : null,
                            roomX = (int?)f.X,
                            roomY = (int?)f.Y,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var roomEntries = await db
                        .RoomEntryLogs.AsNoTracking()
                        .Where(e => e.PlayerEntityId == id)
                        .Where(e =>
                            e.CreatedAt >= profileWindowSince && e.CreatedAt <= profileWindowUntil
                        )
                        .OrderByDescending(e => e.CreatedAt)
                        .Take(12)
                        .Select(e => new
                        {
                            e.CreatedAt,
                            roomId = e.RoomEntityId,
                            roomName = e.RoomEntity != null ? e.RoomEntity.Name : null,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var chatHistory = await db
                        .Chatlogs.AsNoTracking()
                        .Where(c => c.PlayerEntityId == id)
                        .Where(c =>
                            c.CreatedAt >= profileWindowSince && c.CreatedAt <= profileWindowUntil
                        )
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(12)
                        .Select(c => new
                        {
                            c.CreatedAt,
                            roomId = c.RoomEntityId,
                            roomName = c.RoomEntity != null ? c.RoomEntity.Name : null,
                            c.Message,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var itemEvents = await db
                        .ItemEvents.AsNoTracking()
                        .Where(i =>
                            i.ActorPlayerId == playerIdLong
                            || i.FromOwnerId == playerIdLong
                            || i.ToOwnerId == playerIdLong
                        )
                        .Where(i =>
                            i.OccurredAt >= profileWindowSince && i.OccurredAt <= profileWindowUntil
                        )
                        .OrderByDescending(i => i.OccurredAt)
                        .Take(24)
                        .Select(i => new
                        {
                            i.OccurredAt,
                            eventType = i.EventType.ToString(),
                            itemId = i.ItemId,
                            i.RoomId,
                            actorPlayerId = i.ActorPlayerId,
                            fromOwnerId = i.FromOwnerId,
                            toOwnerId = i.ToOwnerId,
                            correlationId = i.CorrelationId,
                            i.Data,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var itemRoomIds = NormalizeIds(itemEvents.Select(i => i.RoomId));

                    var itemRoomNames = await LoadRoomNamesAsync(db, itemRoomIds, ct)
                        .ConfigureAwait(false);

                    var itemPartyIds = NormalizeIds(
                        itemEvents.SelectMany(i =>
                            new[] { i.actorPlayerId, i.fromOwnerId, i.toOwnerId }
                        )
                    );

                    var itemPartyNames = await LoadPlayerNamesAsync(db, itemPartyIds, ct)
                        .ConfigureAwait(false);

                    var itemEventsWithRooms = itemEvents
                        .Select(i => new
                        {
                            i.OccurredAt,
                            i.eventType,
                            i.itemId,
                            i.RoomId,
                            roomName = i.RoomId != null
                            && itemRoomNames.TryGetValue(i.RoomId.Value, out var roomName)
                                ? roomName
                                : null,
                            i.actorPlayerId,
                            actorPlayerName = ResolvePlayerName(itemPartyNames, i.actorPlayerId),
                            i.fromOwnerId,
                            fromOwnerName = ResolvePlayerName(itemPartyNames, i.fromOwnerId),
                            i.toOwnerId,
                            toOwnerName = ResolvePlayerName(itemPartyNames, i.toOwnerId),
                            i.correlationId,
                            i.Data,
                        })
                        .ToList();

                    var auditCount = await db
                        .AuditEvents.AsNoTracking()
                        .Where(a => a.ActorPlayerId == id || a.TargetPlayerId == id)
                        .Where(a =>
                            a.OccurredAt >= profileWindowSince && a.OccurredAt <= profileWindowUntil
                        )
                        .CountAsync(ct)
                        .ConfigureAwait(false);

                    var ledgerCount = await db
                        .EconomyLedger.AsNoTracking()
                        .Where(l => l.PlayerId == id)
                        .Where(l =>
                            l.OccurredAt >= profileWindowSince && l.OccurredAt <= profileWindowUntil
                        )
                        .CountAsync(ct)
                        .ConfigureAwait(false);

                    var itemEventCount = await db
                        .ItemEvents.AsNoTracking()
                        .Where(i =>
                            i.ActorPlayerId == playerIdLong
                            || i.FromOwnerId == playerIdLong
                            || i.ToOwnerId == playerIdLong
                        )
                        .Where(i =>
                            i.OccurredAt >= profileWindowSince && i.OccurredAt <= profileWindowUntil
                        )
                        .CountAsync(ct)
                        .ConfigureAwait(false);

                    var ownedRoomCount = await db
                        .Rooms.AsNoTracking()
                        .Where(r => r.PlayerEntityId == id)
                        .CountAsync(ct)
                        .ConfigureAwait(false);

                    var ownedItemCount = await db
                        .Furnitures.AsNoTracking()
                        .Where(f => f.PlayerEntityId == id)
                        .CountAsync(ct)
                        .ConfigureAwait(false);

                    var playerProfile = player is null
                        ? null
                        : new
                        {
                            player.Id,
                            player.Name,
                            player.Motto,
                            player.Figure,
                            createdAt = player.CreatedAt,
                            updatedAt = player.UpdatedAt,
                            player.status,
                            player.gender,
                            player.perks,
                            window = new { since = profileWindowSince, until = profileWindowUntil },
                            ownedRooms = new { total = ownedRoomCount, latest = ownedRooms },
                            wallets = playerCurrencies,
                            inventory = new { total = ownedItemCount, latest = ownedItems },
                            activity = new
                            {
                                auditEvents = auditCount,
                                ledgerEvents = ledgerCount,
                                itemEvents = itemEventCount,
                            },
                            timeline = new
                            {
                                entries = roomEntries,
                                chats = chatHistory,
                                items = itemEventsWithRooms,
                            },
                        };

                    var audit = db
                        .AuditEvents.AsNoTracking()
                        .Where(a => a.ActorPlayerId == id || a.TargetPlayerId == id);

                    if (since is not null)
                        audit = audit.Where(a => a.OccurredAt >= since.Value);

                    if (until is not null)
                        audit = audit.Where(a => a.OccurredAt <= until.Value);

                    var ledger = db.EconomyLedger.AsNoTracking().Where(l => l.PlayerId == id);

                    if (since is not null)
                        ledger = ledger.Where(l => l.OccurredAt >= since.Value);

                    if (until is not null)
                        ledger = ledger.Where(l => l.OccurredAt <= until.Value);

                    var itemHistory = db
                        .ItemEvents.AsNoTracking()
                        .Where(i =>
                            i.ActorPlayerId == id || i.FromOwnerId == id || i.ToOwnerId == id
                        );

                    if (since is not null)
                        itemHistory = itemHistory.Where(i => i.OccurredAt >= since.Value);

                    if (until is not null)
                        itemHistory = itemHistory.Where(i => i.OccurredAt <= until.Value);

                    var asActorRows = await audit
                        .OrderByDescending(a => a.OccurredAt)
                        .Skip(offset)
                        .Take(limit)
                        .Select(a => new
                        {
                            a.OccurredAt,
                            category = a.Category.ToString(),
                            a.Action,
                            a.ActorPlayerId,
                            a.TargetPlayerId,
                            a.RoomId,
                            a.Data,
                            a.Result,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var actorAndTargetIds = NormalizeIds(
                        asActorRows.SelectMany(r => new[] { r.ActorPlayerId, r.TargetPlayerId })
                    );

                    var actorAndTargetNames = await LoadPlayerNamesAsync(db, actorAndTargetIds, ct)
                        .ConfigureAwait(false);

                    var auditRoomIds = NormalizeIds(asActorRows.Select(r => r.RoomId));

                    var auditRoomNames = await LoadRoomNamesAsync(db, auditRoomIds, ct)
                        .ConfigureAwait(false);

                    var asActor = asActorRows
                        .Select(r => new
                        {
                            r.OccurredAt,
                            r.category,
                            r.Action,
                            r.ActorPlayerId,
                            actorPlayerName = ResolvePlayerName(
                                actorAndTargetNames,
                                r.ActorPlayerId
                            ),
                            r.TargetPlayerId,
                            targetPlayerName = ResolvePlayerName(
                                actorAndTargetNames,
                                r.TargetPlayerId
                            ),
                            r.RoomId,
                            roomName = r.RoomId != null
                            && auditRoomNames.TryGetValue(r.RoomId.Value, out var roomName)
                                ? roomName
                                : null,
                            r.Result,
                            r.Data,
                        })
                        .ToList();

                    var itemHistoryRows = await itemHistory
                        .OrderBy(i => i.OccurredAt)
                        .Skip(offset)
                        .Take(limit)
                        .Select(i => new
                        {
                            i.OccurredAt,
                            eventType = i.EventType.ToString(),
                            i.ItemId,
                            i.RoomId,
                            i.ActorPlayerId,
                            i.FromOwnerId,
                            i.ToOwnerId,
                            i.CorrelationId,
                            i.Data,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var itemHistoryRoomIds = NormalizeIds(
                        itemHistoryRows.Select(row => row.RoomId)
                    );

                    var itemHistoryPartyIds = NormalizeIds(
                        itemHistoryRows.SelectMany(row =>
                            new[] { row.ActorPlayerId, row.FromOwnerId, row.ToOwnerId }
                        )
                    );

                    var itemHistoryRoomNames = await LoadRoomNamesAsync(db, itemHistoryRoomIds, ct)
                        .ConfigureAwait(false);

                    var itemHistoryPartyNames = await LoadPlayerNamesAsync(
                            db,
                            itemHistoryPartyIds,
                            ct
                        )
                        .ConfigureAwait(false);

                    var itemHistoryWithNames = itemHistoryRows
                        .Select(row => new
                        {
                            row.OccurredAt,
                            row.eventType,
                            row.ItemId,
                            row.RoomId,
                            roomName = row.RoomId != null
                            && itemHistoryRoomNames.TryGetValue(row.RoomId.Value, out var roomName)
                                ? roomName
                                : null,
                            row.ActorPlayerId,
                            actorPlayerName = ResolvePlayerName(
                                itemHistoryPartyNames,
                                row.ActorPlayerId
                            ),
                            row.FromOwnerId,
                            fromOwnerName = ResolvePlayerName(
                                itemHistoryPartyNames,
                                row.FromOwnerId
                            ),
                            row.ToOwnerId,
                            toOwnerName = ResolvePlayerName(itemHistoryPartyNames, row.ToOwnerId),
                            row.CorrelationId,
                            row.Data,
                        })
                        .ToList();

                    return new
                    {
                        kind = "id",
                        term,
                        page,
                        limit,
                        offset,
                        asActor,
                        playerProfile,
                        auditTotal = await audit.CountAsync(ct).ConfigureAwait(false),
                        ledger = await ledger
                            .OrderByDescending(l => l.OccurredAt)
                            .Skip(offset)
                            .Take(limit)
                            .Select(l => new
                            {
                                l.OccurredAt,
                                l.Currency,
                                l.Delta,
                                l.BalanceAfter,
                            })
                            .ToListAsync(ct)
                            .ConfigureAwait(false),
                        ledgerTotal = await ledger.CountAsync(ct).ConfigureAwait(false),
                        itemHistory = itemHistoryWithNames,
                        itemTotal = await itemHistory.CountAsync(ct).ConfigureAwait(false),
                    };
                }

                return new
                {
                    kind = "unknown",
                    term,
                    hint = "Enter a player/item id, or a 32-char correlation id.",
                };
            },
            ct
        );

    public Task<object?> RoomTimelineAsync(
        int roomId,
        NameValueCollection query,
        CancellationToken ct
    ) =>
        QueryAsync<object?>(
            async db =>
            {
                var room = await db
                    .Rooms.AsNoTracking()
                    .Where(r => r.Id == roomId)
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.Description,
                        OwnerPlayerId = r.PlayerEntityId,
                        RoomOwnerId = r.PlayerEntityId,
                        RoomOwnerName = r.PlayerEntity != null ? r.PlayerEntity.Name : null,
                        r.UsersNow,
                        r.PlayersMax,
                        LastActive = r.LastActive,
                        ModelName = r.RoomModelEntity.Name,
                    })
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (room is null)
                {
                    return null;
                }

                var limit = ParseLimit(query["limit"], 80, 500);
                var page = ParsePage(query["page"]);
                var offset = Math.Max(0, (page - 1) * limit);
                var take = offset + limit;
                var since = ParseDateTime(query["since"]);
                var until = ParseDateTime(query["until"]);

                var entriesQuery = db
                    .RoomEntryLogs.AsNoTracking()
                    .Where(e => e.RoomEntityId == roomId);
                var chatQuery = db.Chatlogs.AsNoTracking().Where(c => c.RoomEntityId == roomId);
                var itemQuery = db.ItemEvents.AsNoTracking().Where(i => i.RoomId == roomId);

                if (since is not null)
                {
                    entriesQuery = entriesQuery.Where(e => e.CreatedAt >= since.Value);
                    chatQuery = chatQuery.Where(c => c.CreatedAt >= since.Value);
                    itemQuery = itemQuery.Where(i => i.OccurredAt >= since.Value);
                }

                if (until is not null)
                {
                    entriesQuery = entriesQuery.Where(e => e.CreatedAt <= until.Value);
                    chatQuery = chatQuery.Where(c => c.CreatedAt <= until.Value);
                    itemQuery = itemQuery.Where(i => i.OccurredAt <= until.Value);
                }

                var entryCount = await entriesQuery.CountAsync(ct).ConfigureAwait(false);
                var chatCount = await chatQuery.CountAsync(ct).ConfigureAwait(false);
                var itemCount = await itemQuery.CountAsync(ct).ConfigureAwait(false);

                var entryTimeline = await entriesQuery
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(take)
                    .Select(e => new
                    {
                        e.CreatedAt,
                        EventType = "entry",
                        PlayerId = (int?)e.PlayerEntityId,
                        PlayerName = e.PlayerEntity != null ? (string?)e.PlayerEntity.Name : null,
                        Message = (string?)null,
                        TargetPlayerId = (int?)null,
                        TargetPlayerName = (string?)null,
                        ItemId = (long?)null,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var chatTimeline = await chatQuery
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(take)
                    .Select(c => new
                    {
                        c.CreatedAt,
                        EventType = "chat",
                        PlayerId = (int?)c.PlayerEntityId,
                        PlayerName = c.PlayerEntity != null ? (string?)c.PlayerEntity.Name : null,
                        Message = (string?)c.Message,
                        TargetPlayerId = (int?)c.TargetPlayerEntityId,
                        TargetPlayerName = c.TargetPlayerEntity != null
                            ? (string?)c.TargetPlayerEntity.Name
                            : null,
                        ItemId = (long?)null,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var rawItemTimeline = await itemQuery
                    .OrderByDescending(i => i.OccurredAt)
                    .Take(take)
                    .Select(i => new
                    {
                        CreatedAt = i.OccurredAt,
                        EventType = "item",
                        i.ActorPlayerId,
                        Message = (string?)i.Data,
                        i.ToOwnerId,
                        ItemId = (long?)i.ItemId,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var itemTimeline = rawItemTimeline
                    .Select(i => new
                    {
                        i.CreatedAt,
                        i.EventType,
                        PlayerId = ToPlayerId(i.ActorPlayerId),
                        PlayerName = (string?)null,
                        i.Message,
                        TargetPlayerId = ToPlayerId(i.ToOwnerId),
                        TargetPlayerName = (string?)null,
                        i.ItemId,
                    })
                    .ToList();

                var itemPlayerIds = NormalizeIds(
                    itemTimeline.SelectMany(e => new[] { e.PlayerId, e.TargetPlayerId })
                );

                var itemPlayerNames = await LoadPlayerNamesAsync(db, itemPlayerIds, ct)
                    .ConfigureAwait(false);

                var itemTimelineEnriched = itemTimeline
                    .Select(i => new
                    {
                        i.CreatedAt,
                        i.EventType,
                        i.PlayerId,
                        PlayerName = ResolvePlayerName(itemPlayerNames, i.PlayerId) ?? i.PlayerName,
                        i.Message,
                        i.TargetPlayerId,
                        TargetPlayerName = ResolvePlayerName(itemPlayerNames, i.TargetPlayerId)
                            ?? i.TargetPlayerName,
                        i.ItemId,
                    })
                    .ToList();

                var timeline = entryTimeline
                    .Concat(chatTimeline)
                    .Concat(itemTimelineEnriched)
                    .OrderByDescending(e => e.CreatedAt)
                    .ThenBy(e => e.EventType)
                    .Skip(offset)
                    .Take(limit)
                    .ToList();

                return new
                {
                    room = new
                    {
                        roomId = room.Id,
                        room.Name,
                        room.Description,
                        room.RoomOwnerId,
                        room.RoomOwnerName,
                        room.OwnerPlayerId,
                        room.UsersNow,
                        room.PlayersMax,
                        room.LastActive,
                        room.ModelName,
                    },
                    page,
                    limit,
                    offset,
                    count = timeline.Count,
                    total = entryCount + chatCount + itemCount,
                    totals = new
                    {
                        entries = entryCount,
                        chats = chatCount,
                        items = itemCount,
                    },
                    timeline,
                };
            },
            ct
        );

    public Task<object> PlayersAsync(NameValueCollection query, CancellationToken ct)
    {
        var online = _sessionGateway.GetOnlinePlayerIds().Select(p => p.Value).ToHashSet();

        return QueryAsync<object>(
            async db =>
            {
                var term = (query["q"] ?? string.Empty).Trim();
                var limit = ParseLimit(query["limit"], 50, 200);

                List<PlayerRow> rows;

                if (term.Length == 0)
                {
                    // Browsing: surface online players first, then the most recent accounts.
                    var onlineIds = online.ToList();

                    var onlineRows =
                        onlineIds.Count > 0
                            ? await db
                                .Players.AsNoTracking()
                                .Where(p => onlineIds.Contains(p.Id))
                                .OrderBy(p => p.Name)
                                .Take(limit)
                                .Select(p => new PlayerRow(p.Id, p.Name))
                                .ToListAsync(ct)
                                .ConfigureAwait(false)
                            : new List<PlayerRow>();

                    var remaining = limit - onlineRows.Count;

                    var fillRows =
                        remaining > 0
                            ? await db
                                .Players.AsNoTracking()
                                .Where(p => !onlineIds.Contains(p.Id))
                                .OrderByDescending(p => p.Id)
                                .Take(remaining)
                                .Select(p => new PlayerRow(p.Id, p.Name))
                                .ToListAsync(ct)
                                .ConfigureAwait(false)
                            : new List<PlayerRow>();

                    rows = onlineRows.Concat(fillRows).ToList();
                }
                else
                {
                    var players = db.Players.AsNoTracking();

                    if (int.TryParse(term, out var id))
                        players = players.Where(p => p.Name.Contains(term) || p.Id == id);
                    else
                        players = players.Where(p => p.Name.Contains(term));

                    rows = await players
                        .OrderBy(p => p.Name)
                        .Take(limit)
                        .Select(p => new PlayerRow(p.Id, p.Name))
                        .ToListAsync(ct)
                        .ConfigureAwait(false);
                }

                var items = rows.Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        online = online.Contains(p.Id),
                    })
                    .OrderByDescending(p => p.online)
                    .ThenBy(p => p.name)
                    .ToList();

                return new
                {
                    count = items.Count,
                    online = online.Count,
                    items,
                };
            },
            ct
        );
    }

    public Task<object> FurnitureDefinitionsAsync(
        NameValueCollection query,
        CancellationToken ct
    ) =>
        QueryAsync<object>(
            async db =>
            {
                var term = (query["q"] ?? string.Empty).Trim();
                var limit = ParseLimit(query["limit"], 50, 200);

                var definitions = db.FurnitureDefinitions.AsNoTracking();

                if (term.Length > 0)
                {
                    if (int.TryParse(term, out var id))
                        definitions = definitions.Where(f =>
                            f.Name.Contains(term) || f.Id == id || f.SpriteId == id
                        );
                    else
                        definitions = definitions.Where(f => f.Name.Contains(term));
                }

                var rows = await definitions
                    .OrderBy(f => f.Name)
                    .Take(limit)
                    .Select(f => new
                    {
                        f.Id,
                        f.SpriteId,
                        f.Name,
                        type = f.ProductType.ToString(),
                        category = f.FurniCategory.ToString(),
                        f.Width,
                        f.Length,
                        f.CanTrade,
                        f.CanSell,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var items = rows.Select(f => new
                    {
                        id = f.Id,
                        spriteId = f.SpriteId,
                        name = f.Name,
                        type = f.type,
                        category = f.category,
                        width = f.Width,
                        length = f.Length,
                        canTrade = f.CanTrade,
                        canSell = f.CanSell,
                        iconUrl = BuildFurniIconUrl(f.Name),
                    })
                    .ToList();

                return new { count = items.Count, items };
            },
            ct
        );

    private string? BuildFurniIconUrl(string name)
    {
        var template = _config.FurniIconUrlTemplate;

        if (string.IsNullOrWhiteSpace(template) || string.IsNullOrEmpty(name))
            return null;

        return template.Replace("{name}", Uri.EscapeDataString(name), StringComparison.Ordinal);
    }

    private async Task<T> QueryAsync<T>(Func<TurboDbContext, Task<T>> work, CancellationToken ct)
    {
        var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            return await work(db).ConfigureAwait(false);
        }
        finally
        {
            await db.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Row-count totals are full-table scans on tables that grow without bound, so they are cached
    /// for a short interval instead of being recomputed on every overview poll. Concurrent cache
    /// misses simply recompute the same value, so no lock is needed.
    /// </summary>
    private async Task<CachedTotals> GetTotalsAsync(TurboDbContext db, CancellationToken ct)
    {
        var cached = _cachedTotals;

        if (cached is not null && DateTime.UtcNow - cached.AtUtc < TotalsCacheTtl)
            return cached;

        var fresh = new CachedTotals(
            DateTime.UtcNow,
            await db.AuditEvents.CountAsync(ct).ConfigureAwait(false),
            await db.EconomyLedger.CountAsync(ct).ConfigureAwait(false),
            await db.ItemEvents.CountAsync(ct).ConfigureAwait(false),
            await db.PerformanceLogs.CountAsync(ct).ConfigureAwait(false)
        );

        _cachedTotals = fresh;

        return fresh;
    }

    private sealed record CachedTotals(
        DateTime AtUtc,
        long Audit,
        long Ledger,
        long Items,
        long Performance
    );

    private sealed record PlayerRow(int Id, string Name);

    private static List<int> NormalizeIds(IEnumerable<long?> ids) =>
        ids.Select(ToPlayerId)
            .Where(id => id.HasValue)
            .Select(id => id.GetValueOrDefault())
            .Distinct()
            .ToList();

    private static List<int> NormalizeIds(IEnumerable<int?> ids) =>
        ids.Where(id => id.HasValue).Select(id => id.GetValueOrDefault()).Distinct().ToList();

    private static async Task<Dictionary<int, string>> LoadPlayerNamesAsync(
        TurboDbContext db,
        IReadOnlyList<int> playerIds,
        CancellationToken ct
    ) =>
        playerIds.Count == 0
            ? new Dictionary<int, string>()
            : await db
                .Players.AsNoTracking()
                .Where(p => playerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct)
                .ConfigureAwait(false);

    private static async Task<Dictionary<int, string>> LoadRoomNamesAsync(
        TurboDbContext db,
        IReadOnlyList<int> roomIds,
        CancellationToken ct
    ) =>
        roomIds.Count == 0
            ? new Dictionary<int, string>()
            : await db
                .Rooms.AsNoTracking()
                .Where(r => roomIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Name, ct)
                .ConfigureAwait(false);

    private static int ParseLimit(string? value, int fallback, int max) =>
        int.TryParse(value, out var n) ? Math.Clamp(n, 1, max) : fallback;

    private static int ParsePage(string? value)
    {
        if (!int.TryParse(value, out var page))
            return 1;

        return Math.Max(1, page);
    }

    private static int? ToPlayerId(long? playerId)
    {
        if (playerId is null or < int.MinValue or > int.MaxValue)
            return null;

        return (int)playerId.Value;
    }

    private static string? ResolvePlayerName(
        IReadOnlyDictionary<int, string> playerNames,
        long? playerId
    )
    {
        var normalizedPlayerId = ToPlayerId(playerId);

        return
            normalizedPlayerId.HasValue
            && playerNames.TryGetValue(normalizedPlayerId.Value, out var playerName)
            ? playerName
            : null;
    }

    private static string? ResolvePlayerName(
        IReadOnlyDictionary<int, string> playerNames,
        int? playerId
    ) =>
        playerId.HasValue && playerNames.TryGetValue(playerId.Value, out var playerName)
            ? playerName
            : null;

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTimeOffset.TryParse(value, out var parsedOffset))
            return parsedOffset.UtcDateTime;

        if (DateTime.TryParse(value, out var parsedDate))
            return parsedDate;

        return null;
    }
}
