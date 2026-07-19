using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Primitives.Observability;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Read + analytics surface for targeted (personalised/promotional) offers. The admin CRUD lives in
/// <c>DashboardOperationsService.TargetedOffers.cs</c>; here we only read. Purchase analytics come
/// from the <c>economy.targeted_offer_purchase</c> audit trail (see
/// <c>TargetedOfferPurchasedAuditHandler</c>), which carries offerId/identifier/quantity/creditCost/
/// activityPointCost — same shape as <see cref="DashboardApiService.CatalogPurchases.cs"/>.
/// </summary>
internal sealed partial class DashboardApiService
{
    /// <summary>Every targeted offer with its bundle size and lifetime purchase totals, ordered like
    /// the client sees them (sort order then id).</summary>
    public Task<object> TargetedOffersAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                bool activeOnly = string.Equals(
                    query["activeOnly"],
                    "true",
                    StringComparison.OrdinalIgnoreCase
                );

                IQueryable<TargetedOfferEntity> offersQuery = db.TargetedOffers.AsNoTracking();
                if (activeOnly)
                {
                    offersQuery = offersQuery.Where(o => o.Active);
                }

                var rows = await offersQuery
                    .OrderBy(o => o.SortOrder)
                    .ThenBy(o => o.Id)
                    .Select(o => new
                    {
                        o.Id,
                        o.Identifier,
                        o.OfferType,
                        o.Title,
                        o.ImageUrl,
                        o.IconImageUrl,
                        o.ProductCode,
                        o.PriceInCredits,
                        o.PriceInActivityPoints,
                        o.ActivityPointType,
                        o.PurchaseLimit,
                        o.ExpiresAt,
                        o.Active,
                        o.SortOrder,
                        productCount = db.TargetedOfferProducts.Count(p =>
                            p.TargetedOfferEntityId == o.Id
                        ),
                        buyerCount = db.PlayerTargetedOffers.Count(p =>
                            p.TargetedOfferEntityId == o.Id && p.PurchaseCount > 0
                        ),
                        totalPurchases = db.PlayerTargetedOffers.Where(p =>
                                p.TargetedOfferEntityId == o.Id
                            )
                            .Sum(p => (int?)p.PurchaseCount)
                            ?? 0,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                DateTime now = DateTime.UtcNow;
                var items = rows.Select(o => new
                    {
                        o.Id,
                        o.Identifier,
                        o.OfferType,
                        o.Title,
                        o.ImageUrl,
                        o.IconImageUrl,
                        o.ProductCode,
                        o.PriceInCredits,
                        o.PriceInActivityPoints,
                        o.ActivityPointType,
                        o.PurchaseLimit,
                        o.ExpiresAt,
                        expired = o.ExpiresAt is { } expiry && expiry <= now,
                        o.Active,
                        o.SortOrder,
                        o.productCount,
                        o.buyerCount,
                        o.totalPurchases,
                    })
                    .ToList();

                return new { count = items.Count, items };
            },
            ct
        );

    /// <summary>One targeted offer with its full field set and bundle products (furniture name/icon
    /// attached), plus its lifetime purchase totals.</summary>
    public Task<object?> TargetedOfferDetailAsync(int offerId, CancellationToken ct) =>
        QueryAsync<object?>(
            async db =>
            {
                TargetedOfferEntity? offer = await db
                    .TargetedOffers.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == offerId, ct)
                    .ConfigureAwait(false);

                if (offer is null)
                {
                    return null;
                }

                var productRows = await db
                    .TargetedOfferProducts.AsNoTracking()
                    .Where(p => p.TargetedOfferEntityId == offerId)
                    .OrderBy(p => p.Id)
                    .Select(p => new
                    {
                        p.Id,
                        p.ProductCode,
                        p.FurnitureDefinitionEntityId,
                        furnitureName = p.FurnitureDefinition != null
                            ? p.FurnitureDefinition.Name
                            : null,
                        p.Quantity,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // BuildFurniIconUrl isn't SQL-translatable, so icon URLs are attached in a second pass
                // over the materialized rows (same shape as CatalogOfferDetailAsync).
                var products = productRows
                    .Select(p => new
                    {
                        p.Id,
                        p.ProductCode,
                        p.FurnitureDefinitionEntityId,
                        p.furnitureName,
                        furnitureIconUrl = p.furnitureName is null
                            ? null
                            : BuildFurniIconUrl(p.furnitureName),
                        p.Quantity,
                    })
                    .ToList();

                int buyerCount = await db
                    .PlayerTargetedOffers.AsNoTracking()
                    .CountAsync(p => p.TargetedOfferEntityId == offerId && p.PurchaseCount > 0, ct)
                    .ConfigureAwait(false);

                int totalPurchases =
                    await db
                        .PlayerTargetedOffers.AsNoTracking()
                        .Where(p => p.TargetedOfferEntityId == offerId)
                        .SumAsync(p => (int?)p.PurchaseCount, ct)
                        .ConfigureAwait(false)
                    ?? 0;

                DateTime now = DateTime.UtcNow;
                return new
                {
                    offer.Id,
                    offer.Identifier,
                    offer.OfferType,
                    offer.Title,
                    offer.Description,
                    offer.ImageUrl,
                    offer.IconImageUrl,
                    offer.ProductCode,
                    offer.PriceInCredits,
                    offer.PriceInActivityPoints,
                    offer.ActivityPointType,
                    offer.PurchaseLimit,
                    offer.ExpiresAt,
                    expired = offer.ExpiresAt is { } expiry && expiry <= now,
                    offer.Active,
                    offer.SortOrder,
                    buyerCount,
                    totalPurchases,
                    products,
                };
            },
            ct
        );

    /// <summary>
    /// Everything the targeted-offer admin form needs that isn't per-offer: the configured promo-image
    /// base template (so the operator supplies just a filename with a live preview instead of a whole
    /// URL) and the currency types for the activity-point-type picker (the client filters out Credits,
    /// which are already handled by the separate credits price field — no duplicate).
    /// </summary>
    public Task<object> TargetedOfferFormMetaAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var currencyTypes = await db
                    .CurrencyTypes.AsNoTracking()
                    .Where(c => c.Enabled)
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        type = c.CurrencyType.ToString(),
                        c.ActivityPointType,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new { imageTemplate = _assetUrls.TargetedOfferImageTemplate, currencyTypes };
            },
            ct
        );

    /// <summary>
    /// The promo images available under the configured asset root's <c>c_images/targetedoffers</c>
    /// folder, so the offer form can show a gallery to pick from instead of asking the operator to
    /// type a filename they can't know. Returns an empty list when <c>AssetsLocalRoot</c> or the image
    /// template is unset (the form then falls back to manual entry). <c>.thumb.png</c> variants are
    /// folded into their main image's <c>thumbUrl</c> rather than listed separately.
    /// </summary>
    public object TargetedOfferImages()
    {
        string root = _config.AssetsLocalRoot;
        string? template = _assetUrls.TargetedOfferImageTemplate;

        if (string.IsNullOrWhiteSpace(root) || template is null)
        {
            return new { count = 0, items = Array.Empty<object>() };
        }

        string dir = Path.Combine(root, "c_images", "targetedoffers");

        if (!Directory.Exists(dir))
        {
            return new { count = 0, items = Array.Empty<object>() };
        }

        HashSet<string> allFiles = Directory
            .EnumerateFiles(dir)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<object> items = allFiles
            .Where(name =>
                (
                    name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
                ) && !name.EndsWith(".thumb.png", StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name =>
            {
                string thumbName =
                    Path.GetFileNameWithoutExtension(name) + ".thumb" + Path.GetExtension(name);
                string previewName = allFiles.Contains(thumbName) ? thumbName : name;

                return (object)
                    new
                    {
                        file = name,
                        url = template.Replace("{file}", name, StringComparison.Ordinal),
                        thumbUrl = template.Replace(
                            "{file}",
                            previewName,
                            StringComparison.Ordinal
                        ),
                    };
            })
            .ToList();

        return new { count = items.Count, items };
    }

    /// <summary>Purchase volume/revenue over time and the top-selling offers, sourced from the
    /// <c>economy.targeted_offer_purchase</c> audit trail.</summary>
    public Task<object> TargetedOffersStatsAsync(NameValueCollection query, CancellationToken ct) =>
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
                        && a.Action == "economy.targeted_offer_purchase"
                        && a.OccurredAt >= since
                        && a.OccurredAt <= until
                    )
                    .Select(a => new ValueTuple<DateTime, string?>(a.OccurredAt, a.Data))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<TargetedOfferPurchasePayload> purchases = events
                    .Select(e => ParseTargetedOfferPurchasePayload(e.OccurredAt, e.Data))
                    .Where(p => p is not null)
                    .Select(p => p!)
                    .ToList();

                int purchaseCount = purchases.Count;
                long totalCreditsSpent = purchases.Sum(p => (long)p.CreditCost);
                long totalActivityPointsSpent = purchases.Sum(p => (long)p.ActivityPointCost);
                long totalQuantity = purchases.Sum(p => (long)p.Quantity);

                Dictionary<DateTime, (int count, long credits)> bucketMap = new();
                DateTime cursor = ResolveCalendarBucket(since, granularity);
                DateTime end = ResolveCalendarBucket(until, granularity);

                while (cursor <= end)
                {
                    bucketMap[cursor] = (0, 0L);
                    cursor = NextCalendarBucket(cursor, granularity);
                }

                foreach (TargetedOfferPurchasePayload p in purchases)
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
                        identifier = g.Select(p => p.Identifier)
                            .FirstOrDefault(id => !string.IsNullOrEmpty(id)),
                        purchaseCount = g.Count(),
                        quantity = g.Sum(p => (long)p.Quantity),
                        creditsSpent = g.Sum(p => (long)p.CreditCost),
                        activityPointsSpent = g.Sum(p => (long)p.ActivityPointCost),
                    })
                    .OrderByDescending(g => g.creditsSpent)
                    .ThenByDescending(g => g.purchaseCount)
                    .Take(10)
                    .ToList();

                int[] offerIds = topOfferGroups.Select(g => g.offerId).ToArray();
                Dictionary<int, string> offerTitles = await db
                    .TargetedOffers.AsNoTracking()
                    .Where(o => offerIds.Contains(o.Id))
                    .ToDictionaryAsync(
                        o => o.Id,
                        o => string.IsNullOrEmpty(o.Title) ? o.Identifier : o.Title,
                        ct
                    )
                    .ConfigureAwait(false);

                // An offer's icon is its first bundled furniture's icon (BuildFurniIconUrl isn't
                // SQL-translatable, so the first-per-offer pick happens in memory).
                List<TargetedOfferFurni> offerProductFurni = await db
                    .TargetedOfferProducts.AsNoTracking()
                    .Where(p =>
                        offerIds.Contains(p.TargetedOfferEntityId)
                        && p.FurnitureDefinitionEntityId != null
                    )
                    .OrderBy(p => p.Id)
                    .Select(p => new TargetedOfferFurni(
                        p.TargetedOfferEntityId,
                        p.FurnitureDefinition!.Name
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> offerFurniNames = offerProductFurni
                    .GroupBy(p => p.OfferId)
                    .ToDictionary(g => g.Key, g => g.First().Name);

                var topOffers = topOfferGroups
                    .Select(g => new
                    {
                        g.offerId,
                        offerName = offerTitles.GetValueOrDefault(
                            g.offerId,
                            string.IsNullOrEmpty(g.identifier)
                                ? $"offer #{g.offerId}"
                                : g.identifier
                        ),
                        furniIconUrl = offerFurniNames.TryGetValue(g.offerId, out string? furniName)
                            ? BuildFurniIconUrl(furniName)
                            : null,
                        g.purchaseCount,
                        g.quantity,
                        g.creditsSpent,
                        g.activityPointsSpent,
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
                        totalActivityPointsSpent,
                        totalQuantity,
                    },
                    timeline,
                    topOffers,
                };
            },
            ct
        );

    private sealed record TargetedOfferFurni(int OfferId, string Name);

    private sealed record TargetedOfferPurchasePayload(
        DateTime OccurredAt,
        int OfferId,
        string Identifier,
        int Quantity,
        int CreditCost,
        int ActivityPointCost
    );

    private static TargetedOfferPurchasePayload? ParseTargetedOfferPurchasePayload(
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

            string identifier = root.TryGetProperty("identifier", out JsonElement idEl)
                ? idEl.GetString() ?? string.Empty
                : string.Empty;
            int quantity = TryParseInt(root, "quantity") ?? 1;
            int creditCost = TryParseInt(root, "creditCost") ?? 0;
            int activityPointCost = TryParseInt(root, "activityPointCost") ?? 0;

            return new TargetedOfferPurchasePayload(
                occurredAt,
                offerId,
                identifier,
                quantity,
                creditCost,
                activityPointCost
            );
        }
        catch
        {
            // Intentionally ignore malformed payloads.
            return null;
        }
    }
}
