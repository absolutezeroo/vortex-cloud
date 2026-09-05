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
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Database.Context;
using Vortex.Database.Entities.Audit;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Marketplace;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Room;
using Vortex.Observability.Configuration;
using Vortex.Observability.Metrics;
using Vortex.Observability.Runtime;
using Vortex.Primitives.Benchmark;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.Dashboard.API.Api;

internal sealed partial class DashboardApiService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IGrainFactory grainFactory,
    ISessionGateway sessionGateway,
    DashboardAssetUrls assetUrls,
    GamedataDocumentStore gamedata,
    HabbiconArtwork habbiconArtwork,
    RoomPerformanceAggregator roomPerformance,
    IBenchmarkService benchmark,
    IOptions<ObservabilityConfig> options
)
{
    private readonly IDbContextFactory<VortexDbContext> _dbContextFactory = dbContextFactory;
    private readonly RoomPerformanceAggregator _roomPerformance = roomPerformance;
    private readonly DashboardAssetUrls _assetUrls = assetUrls;
    private readonly GamedataDocumentStore _gamedata = gamedata;
    private readonly HabbiconArtwork _habbiconArtwork = habbiconArtwork;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ISessionGateway _sessionGateway = sessionGateway;
    private readonly IBenchmarkService _benchmark = benchmark;
    private readonly ObservabilityConfig _config = options.Value;

    private async Task<T> QueryAsync<T>(Func<VortexDbContext, Task<T>> work, CancellationToken ct)
    {
        VortexDbContext db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            return await work(db).ConfigureAwait(false);
        }
        finally
        {
            await db.DisposeAsync().ConfigureAwait(false);
        }
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
        VortexDbContext db,
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
        VortexDbContext db,
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

    /// <summary>
    ///     Widest span a windowed read will scan. The economy ledger is the fastest-growing table in the
    ///     emulator and the trend reads pull their whole window into memory to bucket it, so an
    ///     unbounded <c>?since=</c> is a heap allocation the size of the ledger -- inside the game
    ///     process. A year of history is more than any trend chart plots.
    /// </summary>
    private const int MAX_WINDOW_DAYS = 366;

    /// <summary>
    ///     Resolves the <c>since</c>/<c>until</c> pair every windowed read takes, defaulting to the last
    ///     <paramref name="defaultSpan" /> (30 days) and refusing anything wider than
    ///     <see cref="MAX_WINDOW_DAYS" />. An inverted pair is swapped rather than refused -- that one is
    ///     unambiguous.
    /// </summary>
    internal static (DateTime Since, DateTime Until) ResolveWindow(
        NameValueCollection query,
        DateTime nowUtc,
        TimeSpan? defaultSpan = null
    )
    {
        DateTime until = ParseDateTime(query["until"]) ?? nowUtc;
        DateTime since =
            ParseDateTime(query["since"]) ?? until - (defaultSpan ?? TimeSpan.FromDays(30));

        if (since > until)
        {
            (since, until) = (until, since);
        }

        if (until - since > TimeSpan.FromDays(MAX_WINDOW_DAYS))
        {
            throw new DashboardQueryException(
                "window_too_large",
                $"since/until must span at most {MAX_WINDOW_DAYS} days."
            );
        }

        return (since, until);
    }

    /// <summary>
    ///     Null for an absent value, the parsed instant for a valid one, and a 400 for anything else.
    ///     Returning null on garbage -- the old behaviour -- drops the filter, which widens the query
    ///     instead of rejecting it.
    /// </summary>
    internal static DateTime? ParseDateTime(string? value)
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

        throw new DashboardQueryException(
            "invalid_date",
            $"'{value}' is not a date the dashboard can parse."
        );
    }
}
