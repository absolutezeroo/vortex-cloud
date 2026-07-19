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
using Vortex.Database.Context;
using Vortex.Database.Entities.Audit;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Marketplace;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Room;
using Vortex.Observability.Configuration;
using Vortex.Observability.Metrics;
using Vortex.Observability.Runtime;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.Dashboard.API.Api;

internal sealed partial class DashboardApiService
{
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
}
