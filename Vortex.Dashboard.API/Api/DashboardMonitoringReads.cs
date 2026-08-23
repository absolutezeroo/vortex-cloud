using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Vortex.Database.Context;
using Vortex.Observability.Metrics;
using Vortex.Observability.Runtime;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// The health, incident and live-traffic reads behind the overview. Split out of
/// <see cref="DashboardApiService" /> because it was the reason that class had fourteen constructor
/// parameters: six of them -- the live stats aggregator, incident detection, infrastructure health,
/// club metrics, client performance metrics and the meter -- were read here and nowhere else, so
/// every one of the thirty other read partials carried them for nothing.
///
/// <para>
/// The row-count cache comes with it. Those COUNT(*)s are full scans of tables that grow without
/// bound, cached for half a minute because the overview polls; concurrent misses simply recompute
/// the same value, so no lock is needed.
/// </para>
/// </summary>
internal sealed class DashboardMonitoringReads(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IGrainFactory grainFactory,
    ISessionGateway sessionGateway,
    ILiveStatsAggregator liveStats,
    IIncidentDetectionService incidentDetection,
    IInfrastructureHealthService infrastructureHealth,
    ClubMetrics clubMetrics,
    ClientPerformanceMetrics clientPerformanceMetrics,
    IVortexMetrics metrics,
    RoomPerformanceAggregator roomPerformance
)
{
    private static readonly TimeSpan TotalsCacheTtl = TimeSpan.FromSeconds(30);

    private readonly IDbContextFactory<VortexDbContext> _dbContextFactory = dbContextFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ISessionGateway _sessionGateway = sessionGateway;
    private readonly ILiveStatsAggregator _liveStats = liveStats;
    private readonly IIncidentDetectionService _incidentDetection = incidentDetection;
    private readonly IInfrastructureHealthService _infrastructureHealth = infrastructureHealth;
    private readonly ClubMetrics _clubMetrics = clubMetrics;
    private readonly ClientPerformanceMetrics _clientPerformanceMetrics = clientPerformanceMetrics;
    private readonly IVortexMetrics _metrics = metrics;
    private readonly RoomPerformanceAggregator _roomPerformance = roomPerformance;

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

    /// <summary>
    /// Room tick and room-directory latency over the live stats window. Synchronous and lock-only —
    /// the samples are already in memory, read off the same meter the Prometheus endpoint exports, so
    /// there is nothing to await and no database involved.
    /// </summary>
    public RoomPerformanceSnapshot RoomPerformance() => _roomPerformance.GetSnapshot();

    public Task<InfrastructureHealthSnapshot> InfrastructureAsync(CancellationToken ct) =>
        _infrastructureHealth.GetStatusAsync(ct);

    public async Task<object> OverviewAsync(DateTime startedAtUtc, CancellationToken ct)
    {
        VortexDbContext db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            InfrastructureHealthSnapshot health = await _infrastructureHealth
                .GetStatusAsync(ct)
                .ConfigureAwait(false);
            IncidentDetectionSnapshot incidents = await _incidentDetection
                .DetectAsync(ct)
                .ConfigureAwait(false);
            LiveStatsSnapshot live = await _liveStats.GetSnapshotAsync().ConfigureAwait(false);
            ImmutableArray<RoomSummarySnapshot> activeRooms;

            using (
                _metrics.MeasureRoomDirectoryCall(nameof(IRoomDirectoryGrain.GetActiveRoomsAsync))
            )
            {
                activeRooms = await _grainFactory
                    .GetRoomDirectoryGrain()
                    .GetActiveRoomsAsync()
                    .ConfigureAwait(false);
            }

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
                    // In-memory since process start (not a DB total) — client performance telemetry
                    // moved to OTel metrics; see ClientPerformanceMetrics.
                    performanceSamplesSinceStart = _clientPerformanceMetrics.TotalSamples,
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

    /// <summary>
    /// Row-count totals are full-table scans on tables that grow without bound, so they are cached
    /// for a short interval instead of being recomputed on every overview poll. Concurrent cache
    /// misses simply recompute the same value, so no lock is needed.
    /// </summary>
    private async Task<CachedTotals> GetTotalsAsync(VortexDbContext db, CancellationToken ct)
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
            await db.ItemEvents.CountAsync(ct).ConfigureAwait(false)
        );

        _cachedTotals = fresh;

        return fresh;
    }

    private sealed record CachedTotals(DateTime AtUtc, long Audit, long Ledger, long Items);
}
