using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Audit;
using Turbo.Database.Entities.Furniture;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Room;
using Turbo.Observability.Configuration;
using Turbo.Observability.Metrics;
using Turbo.Observability.Runtime;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players.Enums;
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
        LiveStatsSnapshot live = await _liveStats.GetSnapshotAsync().ConfigureAwait(false);

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
        TurboDbContext db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            InfrastructureHealthSnapshot health = await _infrastructureHealth
                .GetStatusAsync(ct)
                .ConfigureAwait(false);
            IncidentDetectionSnapshot incidents = await _incidentDetection
                .DetectAsync(ct)
                .ConfigureAwait(false);
            LiveStatsSnapshot live = await _liveStats.GetSnapshotAsync().ConfigureAwait(false);
            ImmutableArray<RoomSummarySnapshot> activeRooms = await _grainFactory
                .GetRoomDirectoryGrain()
                .GetActiveRoomsAsync()
                .ConfigureAwait(false);
            DateTime since = DateTime.UtcNow.AddHours(-1);

            var byCategory = await db
                .AuditEvents.AsNoTracking()
                .Where(a => a.OccurredAt >= since)
                .GroupBy(a => a.Category)
                .Select(g => new { category = g.Key.ToString(), count = g.Count() })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            CachedTotals totals = await GetTotalsAsync(db, ct).ConfigureAwait(false);

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
                int limit = ParseLimit(query["limit"], 50, 500);
                int page = ParsePage(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);

                IQueryable<AuditEventEntity> q = db.AuditEvents.AsNoTracking();

                DateTime? since = ParseDateTime(query["since"]);
                DateTime? until = ParseDateTime(query["until"]);

                if (since is not null)
                {
                    q = q.Where(a => a.OccurredAt >= since.Value);
                }

                if (until is not null)
                {
                    q = q.Where(a => a.OccurredAt <= until.Value);
                }

                if (int.TryParse(query["actor"], out int actor))
                {
                    q = q.Where(a => a.ActorPlayerId == actor);
                }

                if (int.TryParse(query["target"], out int target))
                {
                    q = q.Where(a => a.TargetPlayerId == target);
                }

                if (
                    Enum.TryParse<AuditCategory>(
                        query["category"],
                        ignoreCase: true,
                        out AuditCategory cat
                    )
                )
                {
                    q = q.Where(a => a.Category == cat);
                }

                string? action = query["action"];
                if (!string.IsNullOrWhiteSpace(action))
                {
                    q = q.Where(a => a.Action == action);
                }

                int total = await q.CountAsync(ct).ConfigureAwait(false);

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

                List<int> playerIds = NormalizeIds(
                    rows.SelectMany(a => new[] { a.ActorPlayerId, a.TargetPlayerId })
                );

                Dictionary<int, string> playerNames = await LoadPlayerNamesAsync(db, playerIds, ct)
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

    public Task<object> ModerationStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime nowUtc = DateTime.UtcNow;
                DateTime since = ParseDateTime(query["since"]) ?? nowUtc.AddHours(-24);
                DateTime until = ParseDateTime(query["until"]) ?? nowUtc;

                if (since > until)
                {
                    (since, until) = (until, since);
                }

                if (until - since > TimeSpan.FromDays(60))
                {
                    since = until.AddDays(-60);
                }

                int limit = ParseLimit(query["limit"], 80, 500);
                int page = ParsePage(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);

                IQueryable<AuditEventEntity> q = db
                    .AuditEvents.AsNoTracking()
                    .Where(a => a.Category == AuditCategory.Moderation);

                q = q.Where(a => a.OccurredAt >= since && a.OccurredAt <= until);

                if (int.TryParse(query["actor"], out int actor))
                {
                    q = q.Where(a => a.ActorPlayerId == actor);
                }

                if (int.TryParse(query["target"], out int target))
                {
                    q = q.Where(a => a.TargetPlayerId == target);
                }

                if (int.TryParse(query["room"], out int room))
                {
                    q = q.Where(a => a.RoomId == room);
                }

                string? action = query["action"]?.Trim();
                if (!string.IsNullOrWhiteSpace(action))
                {
                    q = q.Where(a => a.Action == action);
                }

                if (Enum.TryParse<AuditResult>(query["result"], true, out AuditResult resultFilter))
                {
                    q = q.Where(a => a.Result == resultFilter);
                }

                List<ModerationEventRow> events = await q.OrderByDescending(a => a.OccurredAt)
                    .Select(a => new ModerationEventRow(
                        a.Id,
                        a.OccurredAt,
                        a.Action,
                        a.Result.ToString(),
                        a.ActorPlayerId,
                        a.TargetPlayerId,
                        a.RoomId,
                        a.Data,
                        a.CorrelationId
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                int total = events.Count;
                var rows = events
                    .Skip(offset)
                    .Take(limit)
                    .Select(e => new
                    {
                        e.Id,
                        e.OccurredAt,
                        e.Action,
                        e.Result,
                        e.ActorPlayerId,
                        e.TargetPlayerId,
                        e.RoomId,
                        e.Data,
                        e.CorrelationId,
                    })
                    .ToList();

                List<int> playerIds = NormalizeIds(
                    events.SelectMany(e => new[] { e.ActorPlayerId, e.TargetPlayerId })
                );
                List<int> roomIds = NormalizeIds(events.Select(e => e.RoomId));

                Dictionary<int, string> playerNames = await LoadPlayerNamesAsync(db, playerIds, ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> roomNames = await LoadRoomNamesAsync(db, roomIds, ct)
                    .ConfigureAwait(false);

                HashSet<long> renewedBanEventIds = DetectRenewedBanEventIds(events);

                var rowsWithNames = rows.Select(r =>
                    {
                        ModerationEventRow? raw = events.FirstOrDefault(e =>
                            e.Id == r.Id && e.OccurredAt == r.OccurredAt
                        );
                        int? durationSeconds = raw is null
                            ? null
                            : ParseModerationDurationSeconds(raw.Data);

                        return new
                        {
                            r.Id,
                            r.OccurredAt,
                            r.Action,
                            r.Result,
                            r.ActorPlayerId,
                            actorName = ResolvePlayerName(playerNames, r.ActorPlayerId),
                            r.TargetPlayerId,
                            targetName = ResolvePlayerName(playerNames, r.TargetPlayerId),
                            r.RoomId,
                            roomName = r.RoomId != null
                            && roomNames.TryGetValue(r.RoomId.Value, out string? roomName)
                                ? roomName
                                : null,
                            durationSeconds,
                            duration = FormatModerationDuration(durationSeconds),
                            reason = raw is null
                                ? null
                                : SummarizeModerationReason(r.Action, raw.Data),
                            isRenewal = r.Id != 0 && renewedBanEventIds.Contains(r.Id),
                            r.CorrelationId,
                        };
                    })
                    .ToList();

                var byAction = events
                    .GroupBy(e => e.Action)
                    .Select(g => new { action = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .ThenBy(g => g.action)
                    .ToList();

                var byResult = events
                    .GroupBy(e => e.Result)
                    .Select(g => new { result = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .ThenBy(g => g.result)
                    .ToList();

                var topActors = events
                    .Where(e => e.ActorPlayerId is not null)
                    .GroupBy(e => e.ActorPlayerId!.Value)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Take(8)
                    .Select(g => new
                    {
                        actorPlayerId = g.Key,
                        actorName = ResolvePlayerName(playerNames, g.Key),
                        count = g.Count(),
                    })
                    .ToList();

                var topTargets = events
                    .Where(e => e.TargetPlayerId is not null)
                    .GroupBy(e => e.TargetPlayerId!.Value)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Take(8)
                    .Select(g => new
                    {
                        targetPlayerId = g.Key,
                        targetName = ResolvePlayerName(playerNames, g.Key),
                        count = g.Count(),
                    })
                    .ToList();

                var topRooms = events
                    .Where(e => e.RoomId is not null)
                    .GroupBy(e => e.RoomId!.Value)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Take(8)
                    .Select(g => new
                    {
                        roomId = g.Key,
                        roomName = roomNames.TryGetValue(g.Key, out string? roomName)
                            ? roomName
                            : null,
                        count = g.Count(),
                    })
                    .ToList();

                List<int> durations = events
                    .Select(e => ParseModerationDurationSeconds(e.Data))
                    .Where(seconds => seconds.HasValue && seconds.Value > 0)
                    .Select(seconds => seconds!.Value)
                    .ToList();

                int totalBans = await db
                    .RoomBans.AsNoTracking()
                    .CountAsync(ct)
                    .ConfigureAwait(false);
                int activeBans = await db
                    .RoomBans.AsNoTracking()
                    .Where(b => b.DateExpires > nowUtc)
                    .CountAsync(ct)
                    .ConfigureAwait(false);
                int inactiveBans = totalBans - activeBans;

                TimeSpan bucketSize = ResolveBucketSize(since, until);
                List<ModerationTimelinePoint> timeline = BuildModerationTimeline(
                    events,
                    since,
                    until,
                    bucketSize
                );

                return new
                {
                    window = new { since, until },
                    totals = new
                    {
                        total,
                        limit,
                        page,
                        offset,
                        success = byResult
                            .FirstOrDefault(r => r.result == AuditResult.Success.ToString())
                            ?.count
                            ?? 0,
                        denied = byResult
                            .FirstOrDefault(r => r.result == AuditResult.Denied.ToString())
                            ?.count
                            ?? 0,
                        failed = byResult
                            .FirstOrDefault(r => r.result == AuditResult.Failed.ToString())
                            ?.count
                            ?? 0,
                        retentionRate = totalBans > 0
                            ? Math.Round((double)activeBans / totalBans, 4)
                            : 0d,
                        activeBans,
                        inactiveBans,
                        totalBans,
                        renewalCount = renewedBanEventIds.Count,
                        averageDurationSeconds = durations.Count > 0
                            ? Math.Round(durations.Average(), 2)
                            : 0d,
                    },
                    distribution = new { byAction, byResult },
                    timeline,
                    topActors,
                    topTargets,
                    topRooms,
                    rows = rowsWithNames,
                };
            },
            ct
        );

    public Task<object> EconomyAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                int limit = ParseLimit(query["limit"], 50, 500);
                int page = ParsePage(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);
                IQueryable<EconomyLedgerEntity> q = db.EconomyLedger.AsNoTracking();

                DateTime? since = ParseDateTime(query["since"]);
                DateTime? until = ParseDateTime(query["until"]);

                if (since is not null)
                {
                    q = q.Where(l => l.OccurredAt >= since.Value);
                }

                if (until is not null)
                {
                    q = q.Where(l => l.OccurredAt <= until.Value);
                }

                if (int.TryParse(query["player"], out int player))
                {
                    q = q.Where(l => l.PlayerId == player);
                }

                int total = await q.CountAsync(ct).ConfigureAwait(false);

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

                List<int> playerIds = NormalizeIds(rows.Select(l => (long?)l.PlayerId));

                Dictionary<int, string> playerNames = await LoadPlayerNamesAsync(db, playerIds, ct)
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

    public Task<object> ClubSubscriptionsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime nowUtc = DateTime.UtcNow;
                DateTime until = ParseDateTime(query["until"]) ?? nowUtc;
                DateTime since = ParseDateTime(query["since"]) ?? nowUtc.AddDays(-30);

                if (since > until)
                {
                    (since, until) = (until, since);
                }

                if (until - since > TimeSpan.FromDays(365))
                {
                    since = until.AddDays(-365);
                }

                var subscriptions = await db
                    .PlayerSubscriptions.AsNoTracking()
                    .Select(s => new
                    {
                        playerId = s.PlayerEntityId,
                        type = s.SubscriptionType,
                        level = s.Level,
                        s.ExpiresAt,
                        s.TotalMonths,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> playerIds = NormalizeIds(subscriptions.Select(s => (long?)s.playerId));
                Dictionary<int, string> playerNames = await LoadPlayerNamesAsync(db, playerIds, ct)
                    .ConfigureAwait(false);

                var activeSubscriptions = subscriptions.Where(s => s.ExpiresAt > nowUtc).ToList();
                int totalSubscriptions = subscriptions.Count;
                int inactiveCount = totalSubscriptions - activeSubscriptions.Count;
                int expiringIn7Days = activeSubscriptions.Count(s =>
                    s.ExpiresAt <= nowUtc.AddDays(7)
                );
                int expiringIn30Days = activeSubscriptions.Count(s =>
                    s.ExpiresAt <= nowUtc.AddDays(30)
                );
                double activeRate =
                    totalSubscriptions > 0
                        ? Math.Round((double)activeSubscriptions.Count / totalSubscriptions, 4)
                        : 0d;

                var byType = subscriptions
                    .GroupBy(s => s.type)
                    .Select(g =>
                    {
                        var activeByType = g.Where(s => s.ExpiresAt > nowUtc).ToList();
                        double averageRemainingDays =
                            activeByType.Count > 0
                                ? Math.Round(
                                    activeByType
                                        .Select(s => (s.ExpiresAt - nowUtc).TotalDays)
                                        .Average(),
                                    2
                                )
                                : 0d;

                        double averageTotalMonths = Math.Round(
                            g.Select(s => s.TotalMonths).DefaultIfEmpty(0).Average(),
                            2
                        );

                        return new
                        {
                            type = g.Key.ToString(),
                            total = g.Count(),
                            active = activeByType.Count,
                            inactive = g.Count() - activeByType.Count,
                            averageRemainingDays,
                            averageTotalMonths,
                        };
                    })
                    .OrderBy(x => x.type)
                    .ToList();

                var events = await db
                    .AuditEvents.AsNoTracking()
                    .Where(e =>
                        e.Category == AuditCategory.Economy
                        && e.OccurredAt >= since
                        && e.OccurredAt <= until
                        && (
                            e.Action == "economy.hc.purchase"
                            || e.Action == "economy.hc.renew"
                            || e.Action == "economy.hc.expired"
                        )
                    )
                    .Select(e => new
                    {
                        e.OccurredAt,
                        e.Action,
                        e.ActorPlayerId,
                        e.Data,
                    })
                    .OrderBy(e => e.OccurredAt)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<ClubSubscriptionEvent> clubEvents = events
                    .Select(e => new ClubSubscriptionEvent(
                        e.OccurredAt,
                        e.Action,
                        e.ActorPlayerId,
                        e.Data
                    ))
                    .ToList();

                List<int> actorIds = NormalizeIds(clubEvents.Select(e => e.ActorPlayerId));
                Dictionary<int, string> actorNames = await LoadPlayerNamesAsync(db, actorIds, ct)
                    .ConfigureAwait(false);

                var enrichedEvents = clubEvents
                    .Select(e =>
                    {
                        ClubSubscriptionPayload? payload = ParseClubSubscriptionPayload(e.Data);

                        return new
                        {
                            e.OccurredAt,
                            e.Action,
                            actorPlayerId = ToPlayerId(e.ActorPlayerId),
                            actorPlayerName = ResolvePlayerName(
                                actorNames,
                                ToPlayerId(e.ActorPlayerId)
                            ),
                            payload?.Months,
                            payload?.TotalMonths,
                            payload?.CreditCost,
                            payload?.IsRenewal,
                            payload?.IsVip,
                        };
                    })
                    .OrderByDescending(e => e.OccurredAt)
                    .ToList();

                int purchases = clubEvents.Count(e => e.Action == "economy.hc.purchase");
                int renewals = clubEvents.Count(e => e.Action == "economy.hc.renew");
                int expired = clubEvents.Count(e => e.Action == "economy.hc.expired");
                double renewalShare =
                    purchases + renewals > 0
                        ? Math.Round((double)renewals / (purchases + renewals), 4)
                        : 0d;

                TimeSpan bucketSize = ResolveBucketSize(since, until);
                List<SubscriptionTimelinePoint> lifecycle = BuildSubscriptionTimeline(
                    clubEvents.Select(e => (e.OccurredAt, e.Action)).ToList(),
                    since,
                    until,
                    bucketSize
                );

                var byMonths = enrichedEvents
                    .Where(e => e.Months is not null)
                    .GroupBy(e => e.Months!.Value)
                    .Select(g => new
                    {
                        months = g.Key,
                        total = g.Count(),
                        purchases = g.Count(e => e.Action == "economy.hc.purchase"),
                        renewals = g.Count(e => e.Action == "economy.hc.renew"),
                        expired = g.Count(e => e.Action == "economy.hc.expired"),
                    })
                    .OrderBy(g => g.months)
                    .ToList();

                var recentEvents = enrichedEvents
                    .Take(30)
                    .Select(e => new
                    {
                        e.OccurredAt,
                        e.Action,
                        e.actorPlayerId,
                        e.actorPlayerName,
                        e.Months,
                        e.TotalMonths,
                        e.CreditCost,
                        e.IsRenewal,
                        e.IsVip,
                    })
                    .ToList();

                var topExpiring = activeSubscriptions
                    .Where(s => s.ExpiresAt <= nowUtc.AddDays(14))
                    .OrderBy(s => s.ExpiresAt)
                    .Take(10)
                    .Select(s => new
                    {
                        playerId = s.playerId,
                        playerName = ResolvePlayerName(playerNames, s.playerId),
                        type = s.type.ToString(),
                        level = s.level,
                        totalMonths = s.TotalMonths,
                        expiresAt = s.ExpiresAt,
                        remainingDays = Math.Round(
                            Math.Max(0, (s.ExpiresAt - nowUtc).TotalDays),
                            2
                        ),
                    })
                    .ToList();

                return new
                {
                    window = new { since, until },
                    totals = new
                    {
                        totalSubscriptions,
                        activeSubscriptions = activeSubscriptions.Count,
                        inactiveSubscriptions = inactiveCount,
                        expiringIn7Days,
                        expiringIn30Days,
                        activeRate,
                    },
                    byType,
                    topExpiring,
                    lifecycle = new
                    {
                        totals = new
                        {
                            purchases,
                            renewals,
                            expired,
                            renewalShare,
                        },
                        byMonths,
                        recentEvents,
                        timeline = lifecycle,
                    },
                };
            },
            ct
        );

    public Task<object?> ItemAsync(string idText, NameValueCollection query, CancellationToken ct)
    {
        if (!long.TryParse(idText, out long itemId))
        {
            return Task.FromResult<object?>(null);
        }

        return QueryAsync<object?>(
            async db =>
            {
                int limit = ParseLimit(query["limit"], 50, 500);
                int page = ParsePage(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);
                IQueryable<ItemEventEntity> q = db
                    .ItemEvents.AsNoTracking()
                    .Where(i => i.ItemId == itemId);

                DateTime? since = ParseDateTime(query["since"]);
                DateTime? until = ParseDateTime(query["until"]);

                if (since is not null)
                {
                    q = q.Where(i => i.OccurredAt >= since.Value);
                }

                if (until is not null)
                {
                    q = q.Where(i => i.OccurredAt <= until.Value);
                }

                int total = await q.CountAsync(ct).ConfigureAwait(false);

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

                List<int> rowPlayerIds = NormalizeIds(
                    rows.SelectMany(r => new[] { r.ActorPlayerId, r.FromOwnerId, r.ToOwnerId })
                );

                Dictionary<int, string> rowPlayerNames = await LoadPlayerNamesAsync(
                        db,
                        rowPlayerIds,
                        ct
                    )
                    .ConfigureAwait(false);

                List<int> rowRoomIds = NormalizeIds(rows.Select(r => r.RoomId));

                Dictionary<int, string> rowRoomNames = await LoadRoomNamesAsync(db, rowRoomIds, ct)
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
                        && rowRoomNames.TryGetValue(r.RoomId.Value, out string? roomName)
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
                string term = (query["q"] ?? string.Empty).Trim();
                int limit = ParseLimit(query["limit"], 50, 500);
                int page = ParsePage(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);

                DateTime? since = ParseDateTime(query["since"]);
                DateTime? until = ParseDateTime(query["until"]);

                // Correlation id: 32 hex chars (Guid "N").
                if (term.Length == 32 && term.All(Uri.IsHexDigit))
                {
                    IQueryable<AuditEventEntity> audit = db
                        .AuditEvents.AsNoTracking()
                        .Where(a => a.CorrelationId == term);

                    if (since is not null)
                    {
                        audit = audit.Where(a => a.OccurredAt >= since.Value);
                    }

                    if (until is not null)
                    {
                        audit = audit.Where(a => a.OccurredAt <= until.Value);
                    }

                    IQueryable<EconomyLedgerEntity> ledger = db
                        .EconomyLedger.AsNoTracking()
                        .Where(l => l.CorrelationId == term);

                    if (since is not null)
                    {
                        ledger = ledger.Where(l => l.OccurredAt >= since.Value);
                    }

                    if (until is not null)
                    {
                        ledger = ledger.Where(l => l.OccurredAt <= until.Value);
                    }

                    IQueryable<ItemEventEntity> items = db
                        .ItemEvents.AsNoTracking()
                        .Where(i => i.CorrelationId == term);

                    if (since is not null)
                    {
                        items = items.Where(i => i.OccurredAt >= since.Value);
                    }

                    if (until is not null)
                    {
                        items = items.Where(i => i.OccurredAt <= until.Value);
                    }

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

                    List<int> auditActorIds = NormalizeIds(auditRows.Select(a => a.ActorPlayerId));

                    Dictionary<int, string> auditActorNames = await LoadPlayerNamesAsync(
                            db,
                            auditActorIds,
                            ct
                        )
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

                if (int.TryParse(term, out int id))
                {
                    long playerIdLong = (long)id;
                    DateTime profileWindowSince = since ?? DateTime.UtcNow.AddHours(-24);
                    DateTime profileWindowUntil = until ?? DateTime.UtcNow;

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

                    List<int> itemRoomIds = NormalizeIds(itemEvents.Select(i => i.RoomId));

                    Dictionary<int, string> itemRoomNames = await LoadRoomNamesAsync(
                            db,
                            itemRoomIds,
                            ct
                        )
                        .ConfigureAwait(false);

                    List<int> itemPartyIds = NormalizeIds(
                        itemEvents.SelectMany(i =>
                            new[] { i.actorPlayerId, i.fromOwnerId, i.toOwnerId }
                        )
                    );

                    Dictionary<int, string> itemPartyNames = await LoadPlayerNamesAsync(
                            db,
                            itemPartyIds,
                            ct
                        )
                        .ConfigureAwait(false);

                    var itemEventsWithRooms = itemEvents
                        .Select(i => new
                        {
                            i.OccurredAt,
                            i.eventType,
                            i.itemId,
                            i.RoomId,
                            roomName = i.RoomId != null
                            && itemRoomNames.TryGetValue(i.RoomId.Value, out string? roomName)
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

                    int auditCount = await db
                        .AuditEvents.AsNoTracking()
                        .Where(a => a.ActorPlayerId == id || a.TargetPlayerId == id)
                        .Where(a =>
                            a.OccurredAt >= profileWindowSince && a.OccurredAt <= profileWindowUntil
                        )
                        .CountAsync(ct)
                        .ConfigureAwait(false);

                    int ledgerCount = await db
                        .EconomyLedger.AsNoTracking()
                        .Where(l => l.PlayerId == id)
                        .Where(l =>
                            l.OccurredAt >= profileWindowSince && l.OccurredAt <= profileWindowUntil
                        )
                        .CountAsync(ct)
                        .ConfigureAwait(false);

                    int itemEventCount = await db
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

                    int ownedRoomCount = await db
                        .Rooms.AsNoTracking()
                        .Where(r => r.PlayerEntityId == id)
                        .CountAsync(ct)
                        .ConfigureAwait(false);

                    int ownedItemCount = await db
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

                    IQueryable<AuditEventEntity> audit = db
                        .AuditEvents.AsNoTracking()
                        .Where(a => a.ActorPlayerId == id || a.TargetPlayerId == id);

                    if (since is not null)
                    {
                        audit = audit.Where(a => a.OccurredAt >= since.Value);
                    }

                    if (until is not null)
                    {
                        audit = audit.Where(a => a.OccurredAt <= until.Value);
                    }

                    IQueryable<EconomyLedgerEntity> ledger = db
                        .EconomyLedger.AsNoTracking()
                        .Where(l => l.PlayerId == id);

                    if (since is not null)
                    {
                        ledger = ledger.Where(l => l.OccurredAt >= since.Value);
                    }

                    if (until is not null)
                    {
                        ledger = ledger.Where(l => l.OccurredAt <= until.Value);
                    }

                    IQueryable<ItemEventEntity> itemHistory = db
                        .ItemEvents.AsNoTracking()
                        .Where(i =>
                            i.ActorPlayerId == id || i.FromOwnerId == id || i.ToOwnerId == id
                        );

                    if (since is not null)
                    {
                        itemHistory = itemHistory.Where(i => i.OccurredAt >= since.Value);
                    }

                    if (until is not null)
                    {
                        itemHistory = itemHistory.Where(i => i.OccurredAt <= until.Value);
                    }

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

                    List<int> actorAndTargetIds = NormalizeIds(
                        asActorRows.SelectMany(r => new[] { r.ActorPlayerId, r.TargetPlayerId })
                    );

                    Dictionary<int, string> actorAndTargetNames = await LoadPlayerNamesAsync(
                            db,
                            actorAndTargetIds,
                            ct
                        )
                        .ConfigureAwait(false);

                    List<int> auditRoomIds = NormalizeIds(asActorRows.Select(r => r.RoomId));

                    Dictionary<int, string> auditRoomNames = await LoadRoomNamesAsync(
                            db,
                            auditRoomIds,
                            ct
                        )
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
                            && auditRoomNames.TryGetValue(r.RoomId.Value, out string? roomName)
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

                    List<int> itemHistoryRoomIds = NormalizeIds(
                        itemHistoryRows.Select(row => row.RoomId)
                    );

                    List<int> itemHistoryPartyIds = NormalizeIds(
                        itemHistoryRows.SelectMany(row =>
                            new[] { row.ActorPlayerId, row.FromOwnerId, row.ToOwnerId }
                        )
                    );

                    Dictionary<int, string> itemHistoryRoomNames = await LoadRoomNamesAsync(
                            db,
                            itemHistoryRoomIds,
                            ct
                        )
                        .ConfigureAwait(false);

                    Dictionary<int, string> itemHistoryPartyNames = await LoadPlayerNamesAsync(
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
                            && itemHistoryRoomNames.TryGetValue(
                                row.RoomId.Value,
                                out string? roomName
                            )
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

                int limit = ParseLimit(query["limit"], 80, 500);
                int page = ParsePage(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);
                int take = offset + limit;
                DateTime? since = ParseDateTime(query["since"]);
                DateTime? until = ParseDateTime(query["until"]);

                IQueryable<RoomEntryLogEntity> entriesQuery = db
                    .RoomEntryLogs.AsNoTracking()
                    .Where(e => e.RoomEntityId == roomId);
                IQueryable<RoomChatlogEntity> chatQuery = db
                    .Chatlogs.AsNoTracking()
                    .Where(c => c.RoomEntityId == roomId);
                IQueryable<ItemEventEntity> itemQuery = db
                    .ItemEvents.AsNoTracking()
                    .Where(i => i.RoomId == roomId);

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

                int entryCount = await entriesQuery.CountAsync(ct).ConfigureAwait(false);
                int chatCount = await chatQuery.CountAsync(ct).ConfigureAwait(false);
                int itemCount = await itemQuery.CountAsync(ct).ConfigureAwait(false);

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

                List<int> itemPlayerIds = NormalizeIds(
                    itemTimeline.SelectMany(e => new[] { e.PlayerId, e.TargetPlayerId })
                );

                Dictionary<int, string> itemPlayerNames = await LoadPlayerNamesAsync(
                        db,
                        itemPlayerIds,
                        ct
                    )
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
        HashSet<int> online = _sessionGateway.GetOnlinePlayerIds().Select(p => p.Value).ToHashSet();

        return QueryAsync<object>(
            async db =>
            {
                string term = (query["q"] ?? string.Empty).Trim();
                int limit = ParseLimit(query["limit"], 50, 200);

                List<PlayerRow> rows;

                if (term.Length == 0)
                {
                    // Browsing: surface online players first, then the most recent accounts.
                    List<int> onlineIds = online.ToList();

                    List<PlayerRow> onlineRows =
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

                    int remaining = limit - onlineRows.Count;

                    List<PlayerRow> fillRows =
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
                    IQueryable<PlayerEntity> players = db.Players.AsNoTracking();

                    if (int.TryParse(term, out int id))
                    {
                        players = players.Where(p => p.Name.Contains(term) || p.Id == id);
                    }
                    else
                    {
                        players = players.Where(p => p.Name.Contains(term));
                    }

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
                string term = (query["q"] ?? string.Empty).Trim();
                int limit = ParseLimit(query["limit"], 50, 200);

                IQueryable<FurnitureDefinitionEntity> definitions =
                    db.FurnitureDefinitions.AsNoTracking();

                if (term.Length > 0)
                {
                    if (int.TryParse(term, out int id))
                    {
                        definitions = definitions.Where(f =>
                            f.Name.Contains(term) || f.Id == id || f.SpriteId == id
                        );
                    }
                    else
                    {
                        definitions = definitions.Where(f => f.Name.Contains(term));
                    }
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
        string template = _config.FurniIconUrlTemplate;

        if (string.IsNullOrWhiteSpace(template) || string.IsNullOrEmpty(name))
        {
            return null;
        }

        return template.Replace("{name}", Uri.EscapeDataString(name), StringComparison.Ordinal);
    }

    private async Task<T> QueryAsync<T>(Func<TurboDbContext, Task<T>> work, CancellationToken ct)
    {
        TurboDbContext db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

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
        CachedTotals? cached = _cachedTotals;

        if (cached is not null && DateTime.UtcNow - cached.AtUtc < TotalsCacheTtl)
        {
            return cached;
        }

        CachedTotals fresh = new CachedTotals(
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

    private sealed record ClubSubscriptionEvent(
        DateTime OccurredAt,
        string Action,
        long? ActorPlayerId,
        string? Data
    );

    private sealed record ModerationEventRow(
        long Id,
        DateTime OccurredAt,
        string Action,
        string Result,
        long? ActorPlayerId,
        long? TargetPlayerId,
        int? RoomId,
        string? Data,
        string? CorrelationId
    );

    private sealed record ModerationTimelinePoint(string Bucket, string Label, int Count);

    private sealed record SubscriptionTimelinePoint(
        string Bucket,
        string Label,
        int Purchases,
        int Renewals,
        int Expired
    );

    private sealed record ClubSubscriptionPayload(
        int? Months,
        int? TotalMonths,
        int? CreditCost,
        bool? IsVip,
        bool? IsRenewal
    );

    private static HashSet<long> DetectRenewedBanEventIds(IReadOnlyList<ModerationEventRow> events)
    {
        if (events.Count == 0)
        {
            return [];
        }

        Dictionary<(long Actor, long Target, int Room), DateTime> lastBanByKey =
            new Dictionary<(long Actor, long Target, int Room), DateTime>();
        HashSet<long> renewed = new HashSet<long>();

        foreach (
            ModerationEventRow evt in events
                .Where(e => e.Action == "moderation.ban")
                .OrderBy(e => e.OccurredAt)
        )
        {
            if (evt.ActorPlayerId is null || evt.TargetPlayerId is null || evt.RoomId is null)
            {
                continue;
            }

            (long, long, int) key = (
                evt.ActorPlayerId.Value,
                evt.TargetPlayerId.Value,
                evt.RoomId.Value
            );

            if (lastBanByKey.TryGetValue(key, out _))
            {
                renewed.Add(evt.Id);
            }

            lastBanByKey[key] = evt.OccurredAt;
        }

        return renewed;
    }

    private static List<ModerationTimelinePoint> BuildModerationTimeline(
        IReadOnlyList<ModerationEventRow> events,
        DateTime since,
        DateTime until,
        TimeSpan bucketSize
    )
    {
        if (events.Count == 0)
        {
            return [];
        }

        Dictionary<DateTime, int> bucketMap = new Dictionary<DateTime, int>();
        DateTime cursor = ResolveTimelineBucket(since, bucketSize);
        DateTime end = ResolveTimelineBucket(until, bucketSize);

        while (cursor <= end)
        {
            bucketMap[cursor] = 0;
            cursor = cursor.Add(bucketSize);
        }

        foreach (ModerationEventRow evt in events)
        {
            DateTime bucket = ResolveTimelineBucket(evt.OccurredAt, bucketSize);
            bucketMap.TryGetValue(bucket, out int count);
            bucketMap[bucket] = count + 1;
        }

        return bucketMap
            .OrderBy(pair => pair.Key)
            .Select(pair => new ModerationTimelinePoint(
                pair.Key.ToString("O"),
                FormatTimelineLabel(pair.Key, bucketSize),
                pair.Value
            ))
            .ToList();
    }

    private static List<SubscriptionTimelinePoint> BuildSubscriptionTimeline(
        IReadOnlyList<(DateTime OccurredAt, string Action)> events,
        DateTime since,
        DateTime until,
        TimeSpan bucketSize
    )
    {
        if (events.Count == 0)
        {
            return [];
        }

        Dictionary<DateTime, (int purchases, int renewals, int expired)> bucketMap =
            new Dictionary<DateTime, (int purchases, int renewals, int expired)>();

        DateTime cursor = ResolveTimelineBucket(since, bucketSize);
        DateTime end = ResolveTimelineBucket(until, bucketSize);

        while (cursor <= end)
        {
            bucketMap[cursor] = (purchases: 0, renewals: 0, expired: 0);
            cursor = cursor.Add(bucketSize);
        }

        foreach ((DateTime OccurredAt, string Action) evt in events)
        {
            DateTime bucket = ResolveTimelineBucket(evt.OccurredAt, bucketSize);
            (int purchases, int renewals, int expired) counts = bucketMap.TryGetValue(
                bucket,
                out (int purchases, int renewals, int expired) current
            )
                ? current
                : (purchases: 0, renewals: 0, expired: 0);

            counts = evt.Action switch
            {
                "economy.hc.purchase" => (counts.purchases + 1, counts.renewals, counts.expired),
                "economy.hc.renew" => (counts.purchases, counts.renewals + 1, counts.expired),
                "economy.hc.expired" => (counts.purchases, counts.renewals, counts.expired + 1),
                _ => counts,
            };

            bucketMap[bucket] = counts;
        }

        return bucketMap
            .OrderBy(pair => pair.Key)
            .Select(pair => new SubscriptionTimelinePoint(
                pair.Key.ToString("O"),
                FormatTimelineLabel(pair.Key, bucketSize),
                pair.Value.purchases,
                pair.Value.renewals,
                pair.Value.expired
            ))
            .ToList();
    }

    private static TimeSpan ResolveBucketSize(DateTime since, DateTime until)
    {
        TimeSpan span = until - since;

        if (span <= TimeSpan.FromHours(48))
        {
            return TimeSpan.FromHours(1);
        }

        if (span <= TimeSpan.FromDays(14))
        {
            return TimeSpan.FromDays(1);
        }

        return TimeSpan.FromDays(7);
    }

    private static DateTime ResolveTimelineBucket(DateTime value, TimeSpan bucketSize)
    {
        if (bucketSize.Ticks <= 0)
        {
            return value;
        }

        long ticks = value.Ticks - (value.Ticks % bucketSize.Ticks);
        return new DateTime(ticks, value.Kind);
    }

    private static string FormatTimelineLabel(DateTime bucket, TimeSpan bucketSize)
    {
        if (bucketSize < TimeSpan.FromDays(1))
        {
            return bucket.ToString("MM/dd HH:mm");
        }

        if (bucketSize < TimeSpan.FromDays(14))
        {
            return bucket.ToString("MM/dd");
        }

        return bucket.ToString("yyyy/MM/dd");
    }

    private static int? ParseModerationDurationSeconds(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(data);
            if (!doc.RootElement.TryGetProperty("durationSeconds", out JsonElement durationSeconds))
            {
                return null;
            }

            if (
                durationSeconds.ValueKind == JsonValueKind.Number
                && durationSeconds.TryGetInt32(out int parsed)
            )
            {
                return parsed;
            }

            if (
                durationSeconds.ValueKind == JsonValueKind.String
                && int.TryParse(durationSeconds.GetString(), out parsed)
            )
            {
                return parsed;
            }
        }
        catch
        {
            // Intentionally ignore malformed payloads.
        }

        return null;
    }

    private static string? SummarizeModerationReason(string action, string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(data);

            return action switch
            {
                "moderation.denied"
                    when doc.RootElement.TryGetProperty("action", out JsonElement deniedAction) =>
                    deniedAction.GetString(),
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    private static ClubSubscriptionPayload? ParseClubSubscriptionPayload(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(data);
            JsonElement root = doc.RootElement;

            return new ClubSubscriptionPayload(
                TryParseInt(root, "months"),
                TryParseInt(root, "totalMonths"),
                TryParseInt(root, "creditCost"),
                TryParseBool(root, "isVip"),
                TryParseBool(root, "isRenewal")
            );
        }
        catch
        {
            // Intentionally ignore malformed payloads.
            return null;
        }
    }

    private static int? TryParseInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int parsed))
        {
            return parsed;
        }

        if (
            property.ValueKind == JsonValueKind.String
            && int.TryParse(property.GetString(), out parsed)
        )
        {
            return parsed;
        }

        return null;
    }

    private static bool? TryParseBool(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
        {
            return property.GetBoolean();
        }

        if (
            property.ValueKind == JsonValueKind.String
            && bool.TryParse(property.GetString(), out bool parsed)
        )
        {
            return parsed;
        }

        return null;
    }

    private static string? FormatModerationDuration(int? seconds)
    {
        if (seconds is null or <= 0)
        {
            return null;
        }

        int total = Math.Max(0, seconds.Value);
        int minutes = total / 60;
        int hours = minutes / 60;
        int days = hours / 24;

        if (days > 0)
        {
            return $"{days}d {hours % 24}h";
        }

        if (hours > 0)
        {
            return $"{hours}h {minutes % 60}m";
        }

        return $"{minutes}m";
    }

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
        int.TryParse(value, out int n) ? Math.Clamp(n, 1, max) : fallback;

    private static int ParsePage(string? value)
    {
        if (!int.TryParse(value, out int page))
        {
            return 1;
        }

        return Math.Max(1, page);
    }

    private static int? ToPlayerId(long? playerId)
    {
        if (playerId is null or < int.MinValue or > int.MaxValue)
        {
            return null;
        }

        return (int)playerId.Value;
    }

    private static string? ResolvePlayerName(
        IReadOnlyDictionary<int, string> playerNames,
        long? playerId
    )
    {
        int? normalizedPlayerId = ToPlayerId(playerId);

        return
            normalizedPlayerId.HasValue
            && playerNames.TryGetValue(normalizedPlayerId.Value, out string? playerName)
            ? playerName
            : null;
    }

    private static string? ResolvePlayerName(
        IReadOnlyDictionary<int, string> playerNames,
        int? playerId
    ) =>
        playerId.HasValue && playerNames.TryGetValue(playerId.Value, out string? playerName)
            ? playerName
            : null;

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, out DateTimeOffset parsedOffset))
        {
            return parsedOffset.UtcDateTime;
        }

        if (DateTime.TryParse(value, out DateTime parsedDate))
        {
            return parsedDate;
        }

        return null;
    }
}
