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
using Vortex.Primitives.Signals;

namespace Vortex.Dashboard.API.Api;

internal sealed partial class DashboardApiService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IGrainFactory grainFactory,
    ISessionGateway sessionGateway,
    DashboardAssetUrls assetUrls,
    HabbiconArtwork habbiconArtwork,
    ISignalVocabulary signalVocabulary,
    IOptions<ObservabilityConfig> options
)
{
    private readonly IDbContextFactory<VortexDbContext> _dbContextFactory = dbContextFactory;
    private readonly DashboardAssetUrls _assetUrls = assetUrls;
    private readonly HabbiconArtwork _habbiconArtwork = habbiconArtwork;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ISessionGateway _sessionGateway = sessionGateway;
    private readonly ISignalVocabulary _signalVocabulary = signalVocabulary;
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

    private static TimeSpan ResolveBucketSize(DateTime since, DateTime until) =>
        TimeWindow.BucketSize(since, until);

    private static DateTime ResolveTimelineBucket(DateTime value, TimeSpan bucketSize) =>
        TimeWindow.TimelineBucket(value, bucketSize);

    // The parsers live in JsonValues, which the subjects that have left this class use directly.
    private static int? TryParseInt(JsonElement root, string propertyName) =>
        JsonValues.Int(root, propertyName);

    private static bool? TryParseBool(JsonElement root, string propertyName) =>
        JsonValues.Bool(root, propertyName);

    // The implementations live in TimeWindow, which the subjects that have left this class use
    // directly. These stay as the names the remaining topic files call.
    private static string NormalizeGranularity(string? value) => TimeWindow.Granularity(value);

    /// <summary>Calendar-aligned bucket for day/month/year — unlike <see cref="ResolveTimelineBucket"/>
    /// this handles variable-length months/years correctly instead of a fixed tick interval.</summary>
    private static DateTime ResolveCalendarBucket(DateTime value, string granularity) =>
        TimeWindow.Bucket(value, granularity);

    private static DateTime NextCalendarBucket(DateTime bucket, string granularity) =>
        TimeWindow.NextBucket(bucket, granularity);

    private static string FormatCalendarLabel(DateTime bucket, string granularity) =>
        TimeWindow.Label(bucket, granularity);

    private static string FormatTimelineLabel(DateTime bucket, TimeSpan bucketSize) =>
        TimeWindow.TimelineLabel(bucket, bucketSize);

    private static List<int> NormalizeIds(IEnumerable<long?> ids) =>
        DisplayNameQueries.NormalizeIds(ids);

    private static List<int> NormalizeIds(IEnumerable<int?> ids) =>
        DisplayNameQueries.NormalizeIds(ids);

    // The queries themselves live in DisplayNameQueries, which the subjects that have left this
    // class use directly. These two stay as the name the remaining thirty topic files call.
    private static Task<Dictionary<int, string>> LoadPlayerNamesAsync(
        VortexDbContext db,
        IReadOnlyList<int> playerIds,
        CancellationToken ct
    ) => db.PlayerNamesAsync(playerIds, ct);

    private static Task<Dictionary<int, string>> LoadRoomNamesAsync(
        VortexDbContext db,
        IReadOnlyList<int> roomIds,
        CancellationToken ct
    ) => db.RoomNamesAsync(roomIds, ct);

    private static int ParseInt(string? value, int fallback) =>
        int.TryParse(value, out int parsed) ? parsed : fallback;

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

    private static int? ToPlayerId(long? playerId) => DisplayNameQueries.ToPlayerId(playerId);

    private static string? ResolvePlayerName(
        IReadOnlyDictionary<int, string> playerNames,
        long? playerId
    ) => DisplayNameQueries.ResolvePlayerName(playerNames, playerId);

    private static string? ResolvePlayerName(
        IReadOnlyDictionary<int, string> playerNames,
        int? playerId
    ) => DisplayNameQueries.ResolvePlayerName(playerNames, playerId);

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
    ) => TimeWindow.Resolve(query, nowUtc, defaultSpan);

    /// <summary>
    ///     Null for an absent value, the parsed instant for a valid one, and a 400 for anything else.
    ///     Returning null on garbage -- the old behaviour -- drops the filter, which widens the query
    ///     instead of rejecting it.
    /// </summary>
    internal static DateTime? ParseDateTime(string? value) => TimeWindow.ParseDateTime(value);
}
