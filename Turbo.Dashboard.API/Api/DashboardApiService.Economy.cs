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

internal sealed partial class DashboardApiService
{
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

    /// <summary>
    /// Per-currency spend/earn trend over calendar-aligned buckets (day/month/year — not the
    /// fixed-TimeSpan buckets <see cref="ResolveBucketSize"/> uses, since months/years aren't a
    /// fixed duration). Backs the Economy tab's spend charts: "how many credits/silver/emeralds/
    /// activity points were spent per day/month/year". Groups by the ledger's own <c>Currency</c>
    /// string rather than the fixed <see cref="CurrencyType"/> enum — currency display names are
    /// admin-configurable via the <c>currency_types</c> table (see
    /// <c>PlayerWalletGrain.DescribeCurrency</c>), so whatever labels are actually in the ledger
    /// (which may not be the enum names) are what should drive the series.
    /// </summary>
    public Task<object> EconomyTrendsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime until = ParseDateTime(query["until"]) ?? DateTime.UtcNow;
                DateTime since = ParseDateTime(query["since"]) ?? until.AddDays(-30);
                string granularity = NormalizeGranularity(query["granularity"]);

                List<EconomyTrendRow> rows = await db
                    .EconomyLedger.AsNoTracking()
                    .Where(l => l.OccurredAt >= since && l.OccurredAt <= until)
                    .Select(l => new EconomyTrendRow(l.OccurredAt, l.Currency, l.Delta))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                string[] currencies = rows.Select(r => r.Currency)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(c => c, StringComparer.Ordinal)
                    .ToArray();

                var series = currencies
                    .Select(currency => new
                    {
                        currency,
                        points = BuildEconomyTrendPoints(
                            rows.Where(r => r.Currency == currency).ToList(),
                            since,
                            until,
                            granularity
                        ),
                    })
                    .ToList();

                var totals = currencies.ToDictionary(
                    currency => currency,
                    currency =>
                    {
                        List<EconomyTrendRow> currencyRows = rows.Where(r => r.Currency == currency)
                            .ToList();

                        return new
                        {
                            spend = -currencyRows.Where(r => r.Delta < 0).Sum(r => r.Delta),
                            earned = currencyRows.Where(r => r.Delta > 0).Sum(r => r.Delta),
                            net = currencyRows.Sum(r => r.Delta),
                            transactionCount = currencyRows.Count,
                        };
                    }
                );

                var categories = await BuildSpendCategoriesAsync(db, since, until, ct)
                    .ConfigureAwait(false);

                return new
                {
                    window = new
                    {
                        since,
                        until,
                        granularity,
                    },
                    currencies,
                    series,
                    totals,
                    categories,
                };
            },
            ct
        );

    /// <summary>
    /// "What was this spend actually for" — the ledger's own <c>Reason</c> column is just
    /// Debit/Grant/Adjustment, too coarse to answer that. The business context (catalog purchase,
    /// marketplace sale, LTD raffle, voucher, admin grant, ...) lives on the
    /// <see cref="AuditEventEntity"/> published for the same operation, sharing the same ambient
    /// correlation id (see
    /// <c>ChannelEconomyLedger</c>/<c>ChannelAuditSink</c>, both stamp
    /// <c>ITurboContextAccessor.Current.CorrelationId</c> when the caller doesn't set one
    /// explicitly). Joining on that id attributes each debit to its originating action.
    /// </summary>
    private static async Task<List<object>> BuildSpendCategoriesAsync(
        TurboDbContext db,
        DateTime since,
        DateTime until,
        CancellationToken ct
    )
    {
        var spendRows = await (
            from l in db.EconomyLedger.AsNoTracking()
            where
                l.OccurredAt >= since
                && l.OccurredAt <= until
                && l.Delta < 0
                && l.CorrelationId != null
            join a in db.AuditEvents.AsNoTracking()
                on l.CorrelationId equals a.CorrelationId
                into matched
            from a in matched.DefaultIfEmpty()
            select new
            {
                l.Currency,
                l.Delta,
                Action = a != null ? a.Action : null,
            }
        )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return spendRows
            .GroupBy(r => new { r.Currency, Action = r.Action ?? "uncategorized" })
            .Select(g => new
            {
                currency = g.Key.Currency,
                action = g.Key.Action,
                spend = -g.Sum(r => r.Delta),
                transactionCount = g.Count(),
            })
            .OrderByDescending(x => x.spend)
            .Select(x => (object)x)
            .ToList();
    }

    /// <summary>Marketplace sales summary: active/sold counts, credit volume, a sales timeline, and
    /// top sellers by volume. There is no dashboard visibility into the marketplace today.</summary>
    public Task<object> MarketplaceSummaryAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime until = ParseDateTime(query["until"]) ?? DateTime.UtcNow;
                DateTime since = ParseDateTime(query["since"]) ?? until.AddDays(-30);
                string granularity = NormalizeGranularity(query["granularity"]);

                List<(DateTime UpdatedAt, int Price, int SellerId)> sold = await db
                    .MarketplaceOffers.AsNoTracking()
                    .Where(o =>
                        o.State == MarketplaceOfferState.Sold
                        && o.UpdatedAt >= since
                        && o.UpdatedAt <= until
                    )
                    .Select(o => new ValueTuple<DateTime, int, int>(
                        o.UpdatedAt,
                        o.Price,
                        o.SellerEntityId
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                int activeCount = await db
                    .MarketplaceOffers.AsNoTracking()
                    .CountAsync(o => o.State == MarketplaceOfferState.Active, ct)
                    .ConfigureAwait(false);

                Dictionary<DateTime, (int sales, long volume)> bucketMap = new();

                foreach ((DateTime updatedAt, int price, int _) in sold)
                {
                    DateTime bucket = ResolveCalendarBucket(updatedAt, granularity);
                    (int sales, long volume) current = bucketMap.TryGetValue(
                        bucket,
                        out (int sales, long volume) existing
                    )
                        ? existing
                        : (0, 0L);

                    bucketMap[bucket] = (current.sales + 1, current.volume + price);
                }

                var timeline = bucketMap
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new
                    {
                        bucket = pair.Key.ToString("O"),
                        label = FormatCalendarLabel(pair.Key, granularity),
                        sales = pair.Value.sales,
                        volume = pair.Value.volume,
                    })
                    .ToList();

                List<int> sellerIds = NormalizeIds(sold.Select(s => (int?)s.SellerId));
                Dictionary<int, string> sellerNames = await LoadPlayerNamesAsync(db, sellerIds, ct)
                    .ConfigureAwait(false);

                var topSellers = sold.GroupBy(s => s.SellerId)
                    .Select(g => new
                    {
                        sellerId = g.Key,
                        sellerName = ResolvePlayerName(sellerNames, (int?)g.Key),
                        sales = g.Count(),
                        volume = g.Sum(s => (long)s.Price),
                    })
                    .OrderByDescending(s => s.volume)
                    .Take(10)
                    .ToList();

                return new
                {
                    window = new
                    {
                        since,
                        until,
                        granularity,
                    },
                    totals = new
                    {
                        activeListings = activeCount,
                        soldCount = sold.Count,
                        totalVolume = sold.Sum(s => (long)s.Price),
                        averagePrice = sold.Count > 0 ? sold.Average(s => s.Price) : 0,
                    },
                    timeline,
                    topSellers,
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

    public Task<object> RentableSpacesAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                int limit = ParseLimit(query["limit"], 50, 500);
                int page = ParsePage(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);

                DateTime? since = ParseDateTime(query["since"]);
                DateTime? until = ParseDateTime(query["until"]);

                IQueryable<AuditEventEntity> q = db
                    .AuditEvents.AsNoTracking()
                    .Where(a => a.Category == AuditCategory.RentableSpace && a.DeletedAt == null);

                if (since is not null)
                {
                    q = q.Where(a => a.OccurredAt >= since.Value);
                }

                if (until is not null)
                {
                    q = q.Where(a => a.OccurredAt <= until.Value);
                }

                if (int.TryParse(query["player"], out int playerFilter))
                {
                    q = q.Where(a =>
                        a.ActorPlayerId == playerFilter || a.TargetPlayerId == playerFilter
                    );
                }

                if (int.TryParse(query["room"], out int roomFilter))
                {
                    q = q.Where(a => a.RoomId == roomFilter);
                }

                int total = await q.CountAsync(ct).ConfigureAwait(false);

                var rows = await q.OrderByDescending(a => a.OccurredAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(a => new
                    {
                        a.Id,
                        a.OccurredAt,
                        a.Action,
                        a.ActorPlayerId,
                        a.TargetPlayerId,
                        a.RoomId,
                        a.ItemId,
                        a.Data,
                        a.CorrelationId,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                DateTime now = DateTime.UtcNow;

                int activeCount = await db
                    .RoomRentableSpaces.AsNoTracking()
                    .CountAsync(
                        s =>
                            s.RenterPlayerEntityId != null
                            && s.RentedUntil > now
                            && s.DeletedAt == null,
                        ct
                    )
                    .ConfigureAwait(false);

                List<int> playerIds = NormalizeIds(
                    rows.SelectMany(r => new[] { r.ActorPlayerId, r.TargetPlayerId })
                );

                Dictionary<int, string> playerNames = await LoadPlayerNamesAsync(db, playerIds, ct)
                    .ConfigureAwait(false);

                var rowsWithNames = rows.Select(r => new
                    {
                        r.Id,
                        r.OccurredAt,
                        r.Action,
                        r.ActorPlayerId,
                        actorName = ResolvePlayerName(playerNames, r.ActorPlayerId),
                        r.TargetPlayerId,
                        targetName = ResolvePlayerName(playerNames, r.TargetPlayerId),
                        r.RoomId,
                        r.ItemId,
                        r.Data,
                        r.CorrelationId,
                    })
                    .ToList();

                return new
                {
                    activeRentals = activeCount,
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

    private sealed record ClubSubscriptionEvent(
        DateTime OccurredAt,
        string Action,
        long? ActorPlayerId,
        string? Data
    );

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

    private sealed record EconomyTrendRow(DateTime OccurredAt, string Currency, long Delta);

    private static List<EconomyTrendPoint> BuildEconomyTrendPoints(
        IReadOnlyList<EconomyTrendRow> rows,
        DateTime since,
        DateTime until,
        string granularity
    )
    {
        Dictionary<DateTime, (long spend, long earned, int count)> bucketMap = new();

        DateTime cursor = ResolveCalendarBucket(since, granularity);
        DateTime end = ResolveCalendarBucket(until, granularity);

        while (cursor <= end)
        {
            bucketMap[cursor] = (0, 0, 0);
            cursor = NextCalendarBucket(cursor, granularity);
        }

        foreach (EconomyTrendRow row in rows)
        {
            DateTime bucket = ResolveCalendarBucket(row.OccurredAt, granularity);
            (long spend, long earned, int count) current = bucketMap.TryGetValue(
                bucket,
                out (long spend, long earned, int count) existing
            )
                ? existing
                : (0, 0, 0);

            bucketMap[bucket] =
                row.Delta < 0
                    ? (current.spend - row.Delta, current.earned, current.count + 1)
                    : (current.spend, current.earned + row.Delta, current.count + 1);
        }

        return bucketMap
            .OrderBy(pair => pair.Key)
            .Select(pair => new EconomyTrendPoint(
                pair.Key.ToString("O"),
                FormatCalendarLabel(pair.Key, granularity),
                pair.Value.spend,
                pair.Value.earned,
                pair.Value.earned - pair.Value.spend,
                pair.Value.count
            ))
            .ToList();
    }

    private sealed record EconomyTrendPoint(
        string Bucket,
        string Label,
        long Spend,
        long Earned,
        long Net,
        int TransactionCount
    );

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
}
