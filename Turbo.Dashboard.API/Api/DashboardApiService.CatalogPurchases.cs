using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Primitives.Observability;

namespace Turbo.Dashboard.API.Api;

internal sealed partial class DashboardApiService
{
    /// <summary>Read-only overview of catalog purchase activity: volume/revenue over time and top
    /// offers sold. Sourced from the <c>economy.catalog_purchase</c> audit trail (see
    /// <c>CatalogPurchasedAuditHandler</c>), which already carries <c>offerId</c>/<c>quantity</c>/
    /// <c>creditCost</c> in its JSON payload — no new instrumentation needed. Distinct from
    /// <c>CatalogPage</c> in <see cref="DashboardApiService.Catalog.cs"/>-style admin CRUD, which
    /// manages the catalog structure, not purchase analytics.</summary>
    public Task<object> CatalogPurchasesStatsAsync(
        NameValueCollection query,
        CancellationToken ct
    ) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime until = ParseDateTime(query["until"]) ?? DateTime.UtcNow;
                DateTime since = ParseDateTime(query["since"]) ?? until.AddDays(-30);
                string granularity = NormalizeGranularity(query["granularity"]);

                List<(DateTime OccurredAt, string? Data)> events = await db
                    .AuditEvents.AsNoTracking()
                    .Where(a =>
                        a.Category == AuditCategory.Economy
                        && a.Action == "economy.catalog_purchase"
                        && a.OccurredAt >= since
                        && a.OccurredAt <= until
                    )
                    .Select(a => new ValueTuple<DateTime, string?>(a.OccurredAt, a.Data))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<CatalogPurchasePayload> purchases = events
                    .Select(e => ParseCatalogPurchasePayload(e.OccurredAt, e.Data))
                    .Where(p => p is not null)
                    .Select(p => p!)
                    .ToList();

                int purchaseCount = purchases.Count;
                long totalCreditsSpent = purchases.Sum(p => (long)p.CreditCost);
                long totalQuantity = purchases.Sum(p => (long)p.Quantity);

                Dictionary<DateTime, (int count, long credits)> bucketMap = new();
                DateTime cursor = ResolveCalendarBucket(since, granularity);
                DateTime end = ResolveCalendarBucket(until, granularity);

                while (cursor <= end)
                {
                    bucketMap[cursor] = (0, 0L);
                    cursor = NextCalendarBucket(cursor, granularity);
                }

                foreach (CatalogPurchasePayload p in purchases)
                {
                    DateTime bucket = ResolveCalendarBucket(p.OccurredAt, granularity);
                    (int count, long credits) current = bucketMap.GetValueOrDefault(bucket);
                    bucketMap[bucket] = (current.count + 1, current.credits + p.CreditCost);
                }

                var timeline = bucketMap
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new
                    {
                        bucket = pair.Key.ToString("O"),
                        label = FormatCalendarLabel(pair.Key, granularity),
                        purchaseCount = pair.Value.count,
                        creditsSpent = pair.Value.credits,
                    })
                    .ToList();

                var topOfferGroups = purchases
                    .GroupBy(p => p.OfferId)
                    .Select(g => new
                    {
                        offerId = g.Key,
                        purchaseCount = g.Count(),
                        quantity = g.Sum(p => (long)p.Quantity),
                        creditsSpent = g.Sum(p => (long)p.CreditCost),
                        catalogType = g.First().CatalogType,
                    })
                    .OrderByDescending(g => g.creditsSpent)
                    .Take(10)
                    .ToList();

                int[] offerIds = topOfferGroups.Select(g => g.offerId).ToArray();
                Dictionary<int, string> offerNames = await db
                    .CatalogOffers.AsNoTracking()
                    .Where(o => offerIds.Contains(o.Id))
                    .ToDictionaryAsync(o => o.Id, o => o.LocalizationId, ct)
                    .ConfigureAwait(false);

                var topOffers = topOfferGroups
                    .Select(g => new
                    {
                        g.offerId,
                        offerName = offerNames.GetValueOrDefault(g.offerId, $"offer #{g.offerId}"),
                        g.catalogType,
                        g.purchaseCount,
                        g.quantity,
                        g.creditsSpent,
                    })
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
                        purchaseCount,
                        totalCreditsSpent,
                        totalQuantity,
                    },
                    timeline,
                    topOffers,
                };
            },
            ct
        );

    private sealed record CatalogPurchasePayload(
        DateTime OccurredAt,
        string CatalogType,
        int OfferId,
        int Quantity,
        int CreditCost
    );

    private static CatalogPurchasePayload? ParseCatalogPurchasePayload(
        DateTime occurredAt,
        string? data
    )
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(data);
            JsonElement root = doc.RootElement;

            if (
                !root.TryGetProperty("offerId", out JsonElement offerIdEl)
                || !offerIdEl.TryGetInt32(out int offerId)
            )
            {
                return null;
            }

            string catalogType = root.TryGetProperty("catalogType", out JsonElement typeEl)
                ? typeEl.GetString() ?? "Normal"
                : "Normal";
            int quantity = TryParseInt(root, "quantity") ?? 1;
            int creditCost = TryParseInt(root, "creditCost") ?? 0;

            return new CatalogPurchasePayload(
                occurredAt,
                catalogType,
                offerId,
                quantity,
                creditCost
            );
        }
        catch
        {
            // Intentionally ignore malformed payloads.
            return null;
        }
    }
}
