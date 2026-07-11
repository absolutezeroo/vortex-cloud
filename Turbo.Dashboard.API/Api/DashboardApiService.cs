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
using Turbo.Database.Entities.Marketplace;
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

internal sealed partial class DashboardApiService(
    IDbContextFactory<TurboDbContext> dbContextFactory,
    IGrainFactory grainFactory,
    ISessionGateway sessionGateway,
    ILiveStatsAggregator liveStats,
    IIncidentDetectionService incidentDetection,
    IInfrastructureHealthService infrastructureHealth,
    ClubMetrics clubMetrics,
    ClientPerformanceMetrics clientPerformanceMetrics,
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
    private readonly ClientPerformanceMetrics _clientPerformanceMetrics = clientPerformanceMetrics;
    private readonly ObservabilityConfig _config = options.Value;

    private static readonly TimeSpan TotalsCacheTtl = TimeSpan.FromSeconds(30);
    private volatile CachedTotals? _cachedTotals;

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
            await db.ItemEvents.CountAsync(ct).ConfigureAwait(false)
        );

        _cachedTotals = fresh;

        return fresh;
    }

    private sealed record CachedTotals(DateTime AtUtc, long Audit, long Ledger, long Items);

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

    private static string NormalizeGranularity(string? value) =>
        (value ?? "").ToLowerInvariant() switch
        {
            "month" => "month",
            "year" => "year",
            _ => "day",
        };

    /// <summary>Calendar-aligned bucket for day/month/year — unlike <see cref="ResolveTimelineBucket"/>
    /// this handles variable-length months/years correctly instead of a fixed tick interval.</summary>
    private static DateTime ResolveCalendarBucket(DateTime value, string granularity) =>
        granularity switch
        {
            "year" => new DateTime(value.Year, 1, 1, 0, 0, 0, value.Kind),
            "month" => new DateTime(value.Year, value.Month, 1, 0, 0, 0, value.Kind),
            _ => value.Date,
        };

    private static DateTime NextCalendarBucket(DateTime bucket, string granularity) =>
        granularity switch
        {
            "year" => bucket.AddYears(1),
            "month" => bucket.AddMonths(1),
            _ => bucket.AddDays(1),
        };

    private static string FormatCalendarLabel(DateTime bucket, string granularity) =>
        granularity switch
        {
            "year" => bucket.ToString("yyyy"),
            "month" => bucket.ToString("yyyy-MM"),
            _ => bucket.ToString("yyyy-MM-dd"),
        };

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
