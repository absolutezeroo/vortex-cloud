using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Dashboard.API.Api.Safety.Contracts;
using Vortex.Database.Context;
using Vortex.Primitives.Observability;

namespace Vortex.Dashboard.API.Api.Safety;

/// <summary>
/// Reads the item journal for lives that do not add up.
/// </summary>
/// <remarks>
/// <para>
/// <c>item_events</c> was built for this — its own summary says it is indexed by item id "so the
/// complete story of a furniture id can be reconstructed for forensics and duplication
/// investigation" — and until now nothing read it that way. The vocabulary even declares
/// <see cref="ItemEventType.AnomalyFlagged"/>, which nothing emits.
/// </para>
/// <para>
/// Nothing here writes that flag either, and that is deliberate: a signature firing is a reason to
/// go and look, not a verdict. A legitimate re-grant after a rollback looks exactly like a second
/// creation from the outside, and stamping the journal with a conclusion the journal cannot support
/// would make the next investigator trust it.
/// </para>
/// </remarks>
internal sealed class ItemAnomalyReads(IDbContextFactory<VortexDbContext> dbContextFactory)
    : DashboardReads(dbContextFactory)
{
    public Task<ItemAnomalyScan> ScanAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<ItemAnomalyScan>(
            async db =>
            {
                // A window is mandatory, not a convenience: item_events grows without bound and a
                // full scan is a table lock waiting for a busy afternoon. Seven days is what an
                // incident is usually about; the caller widens it deliberately.
                DateTime until = TimeWindow.ParseDateTime(query["until"]) ?? DateTime.UtcNow;
                DateTime since = TimeWindow.ParseDateTime(query["since"]) ?? until.AddDays(-7);
                int limit = QueryValues.Limit(query["limit"], 100, 500);

                IQueryable<Database.Entities.Audit.ItemEventEntity> window = db
                    .ItemEvents.AsNoTracking()
                    .Where(e => e.OccurredAt >= since && e.OccurredAt <= until);

                int itemsScanned = await window
                    .Select(e => e.ItemId)
                    .Distinct()
                    .CountAsync(ct)
                    .ConfigureAwait(false);

                List<ItemAnomaly> found =
                [
                    .. await BornTwiceAsync(window, limit, ct).ConfigureAwait(false),
                    .. await ResurrectedAsync(window, limit, ct).ConfigureAwait(false),
                    .. await TwoPlacesAsync(window, limit, ct).ConfigureAwait(false),
                ];

                // Loudest first: an item that fired a signature nine times is where to start, and an
                // investigator reads down the list rather than searching it.
                List<ItemAnomaly> ranked =
                [
                    .. found
                        .OrderByDescending(a => a.Occurrences)
                        .ThenByDescending(a => a.LastSeen)
                        .Take(limit),
                ];

                return new ItemAnomalyScan(
                    since,
                    until,
                    itemsScanned,
                    ranked.Count,
                    await NameAsync(db, ranked, ct).ConfigureAwait(false)
                );
            },
            ct
        );

    /// <summary>The same item brought into existence more than once.</summary>
    private static async Task<List<ItemAnomaly>> BornTwiceAsync(
        IQueryable<Database.Entities.Audit.ItemEventEntity> window,
        int limit,
        CancellationToken ct
    ) =>
        [
            .. (
                await window
                    .Where(e =>
                        e.EventType == ItemEventType.Created
                        || e.EventType == ItemEventType.CatalogPurchase
                    )
                    .GroupBy(e => e.ItemId)
                    .Where(g => g.Count() > 1)
                    .Select(g => new
                    {
                        ItemId = g.Key,
                        Count = g.Count(),
                        First = g.Min(e => e.OccurredAt),
                        Last = g.Max(e => e.OccurredAt),
                    })
                    .OrderByDescending(g => g.Count)
                    .Take(limit)
                    .ToListAsync(ct)
                    .ConfigureAwait(false)
            ).Select(g => new ItemAnomaly(
                g.ItemId,
                "born_twice",
                $"{g.Count} creation events for one item id.",
                g.Count,
                g.First,
                g.Last,
                null,
                null,
                null,
                null
            )),
        ];

    /// <summary>An item that kept living after it was deleted.</summary>
    private static async Task<List<ItemAnomaly>> ResurrectedAsync(
        IQueryable<Database.Entities.Audit.ItemEventEntity> window,
        int limit,
        CancellationToken ct
    )
    {
        // The last deletion per item, then anything recorded after it. Two queries rather than a
        // correlated subquery: the second one only looks at ids the first found.
        List<(long ItemId, DateTime DeletedAt)> deletions =
        [
            .. (
                await window
                    .Where(e => e.EventType == ItemEventType.Deleted)
                    .GroupBy(e => e.ItemId)
                    .Select(g => new { ItemId = g.Key, DeletedAt = g.Max(e => e.OccurredAt) })
                    .Take(limit * 4)
                    .ToListAsync(ct)
                    .ConfigureAwait(false)
            ).Select(g => (g.ItemId, g.DeletedAt)),
        ];

        if (deletions.Count == 0)
        {
            return [];
        }

        Dictionary<long, DateTime> deletedAt = deletions.ToDictionary(
            d => d.ItemId,
            d => d.DeletedAt
        );
        List<long> ids = [.. deletedAt.Keys];

        var after = await window
            .Where(e => ids.Contains(e.ItemId) && e.EventType != ItemEventType.Deleted)
            .Select(e => new { e.ItemId, e.OccurredAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return
        [
            .. after
                .Where(e => e.OccurredAt > deletedAt[e.ItemId])
                .GroupBy(e => e.ItemId)
                .Select(g => new ItemAnomaly(
                    g.Key,
                    "resurrected",
                    $"{g.Count()} events after the item was deleted on {deletedAt[g.Key]:u}.",
                    g.Count(),
                    g.Min(e => e.OccurredAt),
                    g.Max(e => e.OccurredAt),
                    null,
                    null,
                    null,
                    null
                )),
        ];
    }

    /// <summary>
    /// One item placed in a second room without being picked up from the first.
    /// </summary>
    /// <remarks>
    /// The ordering has to be walked rather than grouped, so this reads the placement and pickup
    /// events for the window and steps through them per item in memory. Only those two event types,
    /// which is what keeps it affordable.
    /// </remarks>
    private static async Task<List<ItemAnomaly>> TwoPlacesAsync(
        IQueryable<Database.Entities.Audit.ItemEventEntity> window,
        int limit,
        CancellationToken ct
    )
    {
        var moves = await window
            .Where(e =>
                e.EventType == ItemEventType.Placed || e.EventType == ItemEventType.PickedUp
            )
            .OrderBy(e => e.ItemId)
            .ThenBy(e => e.OccurredAt)
            .Select(e => new
            {
                e.ItemId,
                e.OccurredAt,
                e.EventType,
                e.RoomId,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<ItemAnomaly> found = [];

        foreach (var item in moves.GroupBy(e => e.ItemId))
        {
            int? standingIn = null;
            int doubled = 0;
            DateTime first = default;
            DateTime last = default;

            foreach (var move in item)
            {
                if (move.EventType == ItemEventType.PickedUp)
                {
                    standingIn = null;
                    continue;
                }

                // Placed while already standing somewhere else, with no pickup in between.
                if (standingIn is { } room && move.RoomId != room)
                {
                    doubled++;
                    first = doubled == 1 ? move.OccurredAt : first;
                    last = move.OccurredAt;
                }

                standingIn = move.RoomId;
            }

            if (doubled > 0)
            {
                found.Add(
                    new ItemAnomaly(
                        item.Key,
                        "two_places",
                        $"Placed in another room {doubled} time(s) without being picked up first.",
                        doubled,
                        first,
                        last,
                        null,
                        null,
                        null,
                        null
                    )
                );
            }
        }

        return [.. found.OrderByDescending(a => a.Occurrences).Take(limit)];
    }

    /// <summary>
    /// Attaches what each flagged item currently is and who holds it.
    /// </summary>
    /// <remarks>
    /// One query for the whole page rather than one per row, and a left join in spirit: an item that
    /// no longer exists keeps its anomaly. A deleted duplicate is still a duplicate that was spent.
    /// </remarks>
    private static async Task<List<ItemAnomaly>> NameAsync(
        VortexDbContext db,
        List<ItemAnomaly> anomalies,
        CancellationToken ct
    )
    {
        if (anomalies.Count == 0)
        {
            return anomalies;
        }

        List<int> ids =
        [
            .. anomalies
                .Select(a => a.ItemId)
                .Where(id => id is > 0 and <= int.MaxValue)
                .Select(id => (int)id)
                .Distinct(),
        ];

        var rows = await db
            .Furnitures.AsNoTracking()
            .Where(f => ids.Contains(f.Id))
            .Select(f => new
            {
                f.Id,
                DefinitionId = (int?)f.FurnitureDefinitionEntityId,
                DefinitionName = f.FurnitureDefinitionEntity != null
                    ? f.FurnitureDefinitionEntity.Name
                    : null,
                OwnerId = (int?)f.PlayerEntityId,
                OwnerName = f.PlayerEntity != null ? f.PlayerEntity.Name : null,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byId = rows.ToDictionary(r => (long)r.Id);

        return
        [
            .. anomalies.Select(a =>
                byId.TryGetValue(a.ItemId, out var row)
                    ? a with
                    {
                        DefinitionId = row.DefinitionId,
                        DefinitionName = row.DefinitionName,
                        OwnerPlayerId = row.OwnerId,
                        OwnerName = row.OwnerName,
                    }
                    : a
            ),
        ];
    }
}
