using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Audit;
using Turbo.Database.Entities.Catalog;
using Turbo.Database.Entities.Furniture;
using Turbo.Database.Entities.Marketplace;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Room;
using Turbo.Observability.Configuration;
using Turbo.Observability.Metrics;
using Turbo.Observability.Runtime;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.Dashboard.API.Api;

internal sealed partial class DashboardApiService
{
    /// <summary>Pages at one level of one catalog tree. <c>parentId</c> omitted/blank means the root
    /// level (pages with no parent) of the given <c>catalogType</c> (0=Normal, 1=BuildersClub).</summary>
    public Task<object> CatalogPagesAsync(NameValueCollection query, CancellationToken ct)
    {
        CatalogType catalogType = int.TryParse(query["catalogType"], out int catalogTypeValue)
            ? (CatalogType)catalogTypeValue
            : CatalogType.Normal;
        int? parentId = int.TryParse(query["parentId"], out int parsedParentId)
            ? parsedParentId
            : null;

        return QueryAsync<object>(
            async db =>
            {
                var rows = await db
                    .CatalogPages.AsNoTracking()
                    .Where(p => p.CatalogType == catalogType && p.ParentEntityId == parentId)
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Localization)
                    .Select(p => new
                    {
                        p.Id,
                        p.ParentEntityId,
                        p.Localization,
                        p.Name,
                        p.Icon,
                        layout = p.Layout.ToLayoutString(),
                        p.SortOrder,
                        p.Visible,
                        childCount = db.CatalogPages.Count(c => c.ParentEntityId == p.Id),
                        offerCount = db.CatalogOffers.Count(o => o.CatalogPageEntityId == p.Id),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var items = rows.Select(p => new
                    {
                        p.Id,
                        p.ParentEntityId,
                        p.Localization,
                        p.Name,
                        p.Icon,
                        iconUrl = BuildCatalogIconUrl(p.Icon),
                        p.layout,
                        p.SortOrder,
                        p.Visible,
                        p.childCount,
                        p.offerCount,
                    })
                    .ToList();

                return new
                {
                    catalogType = (int)catalogType,
                    parentId,
                    count = items.Count,
                    items,
                };
            },
            ct
        );
    }

    public Task<object?> CatalogPageDetailAsync(int pageId, CancellationToken ct) =>
        QueryAsync<object?>(
            async db =>
            {
                CatalogPageEntity? page = await db
                    .CatalogPages.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == pageId, ct)
                    .ConfigureAwait(false);

                if (page is null)
                {
                    return null;
                }

                var offerRows = await db
                    .CatalogOffers.AsNoTracking()
                    .Where(o => o.CatalogPageEntityId == pageId)
                    .OrderBy(o => o.Id)
                    .Select(o => new
                    {
                        o.Id,
                        o.LocalizationId,
                        o.CostCredits,
                        o.CostCurrency,
                        o.CurrencyTypeId,
                        currencyName = o.CurrencyTypeEntity != null
                            ? o.CurrencyTypeEntity.Name
                            : null,
                        o.CanGift,
                        o.CanBundle,
                        o.ClubLevel,
                        o.DiscountPercent,
                        o.Visible,
                        productCount = db.CatalogProducts.Count(pr =>
                            pr.CatalogOfferEntityId == o.Id
                        ),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // Most offers hold exactly one product (a bundle with 2+ is the exception, e.g. a
                // table+chair set) -- fetching that single product's summary here lets the UI show
                // "what you actually get" inline on the offer row, with no extra click/request per
                // row. This is one batched query for every single-product offer on the page, not
                // one query per offer.
                List<int> singleProductOfferIds = offerRows
                    .Where(o => o.productCount == 1)
                    .Select(o => o.Id)
                    .ToList();

                var singleProductRows =
                    singleProductOfferIds.Count == 0
                        ? []
                        : await db
                            .CatalogProducts.AsNoTracking()
                            .Where(p => singleProductOfferIds.Contains(p.CatalogOfferEntityId))
                            .Select(p => new
                            {
                                p.CatalogOfferEntityId,
                                p.Id,
                                productType = (int)p.ProductType,
                                productTypeLabel = p.ProductType.ToString(),
                                furnitureName = p.FurnitureDefinition != null
                                    ? p.FurnitureDefinition.Name
                                    : null,
                                p.Quantity,
                                p.UniqueSize,
                                p.UniqueRemaining,
                                p.BuildersClubEligible,
                            })
                            .ToListAsync(ct)
                            .ConfigureAwait(false);

                Dictionary<int, object> singleProductByOfferId = singleProductRows.ToDictionary(
                    p => p.CatalogOfferEntityId,
                    p =>
                        (object)
                            new
                            {
                                p.Id,
                                p.productType,
                                p.productTypeLabel,
                                p.furnitureName,
                                furnitureIconUrl = p.furnitureName is null
                                    ? null
                                    : BuildFurniIconUrl(p.furnitureName),
                                p.Quantity,
                                p.UniqueSize,
                                p.UniqueRemaining,
                                p.BuildersClubEligible,
                            }
                );

                var offers = offerRows
                    .Select(o => new
                    {
                        o.Id,
                        o.LocalizationId,
                        o.CostCredits,
                        o.CostCurrency,
                        o.CurrencyTypeId,
                        o.currencyName,
                        o.CanGift,
                        o.CanBundle,
                        o.ClubLevel,
                        o.DiscountPercent,
                        o.Visible,
                        o.productCount,
                        singleProduct = singleProductByOfferId.GetValueOrDefault(o.Id),
                    })
                    .ToList();

                string? parentLocalization = page.ParentEntityId is { } parentId
                    ? await db
                        .CatalogPages.AsNoTracking()
                        .Where(p => p.Id == parentId)
                        .Select(p => p.Localization)
                        .FirstOrDefaultAsync(ct)
                        .ConfigureAwait(false)
                    : null;

                return new
                {
                    page.Id,
                    catalogType = (int)page.CatalogType,
                    page.ParentEntityId,
                    parentLocalization,
                    page.Localization,
                    page.Name,
                    page.Icon,
                    iconUrl = BuildCatalogIconUrl(page.Icon),
                    layout = page.Layout.ToLayoutString(),
                    page.ImageData,
                    page.TextData,
                    page.SortOrder,
                    page.Visible,
                    offers,
                };
            },
            ct
        );

    public Task<object?> CatalogOfferDetailAsync(int offerId, CancellationToken ct) =>
        QueryAsync<object?>(
            async db =>
            {
                var offer = await db
                    .CatalogOffers.AsNoTracking()
                    .Where(o => o.Id == offerId)
                    .Select(o => new
                    {
                        o.Id,
                        o.CatalogPageEntityId,
                        pageLocalization = o.Page.Localization,
                        catalogType = (int)o.Page.CatalogType,
                        o.LocalizationId,
                        o.CostCredits,
                        o.CostCurrency,
                        o.CurrencyTypeId,
                        currencyName = o.CurrencyTypeEntity != null
                            ? o.CurrencyTypeEntity.Name
                            : null,
                        o.CanGift,
                        o.CanBundle,
                        o.ClubLevel,
                        o.DiscountPercent,
                        o.Visible,
                    })
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (offer is null)
                {
                    return null;
                }

                var productRows = await db
                    .CatalogProducts.AsNoTracking()
                    .Where(p => p.CatalogOfferEntityId == offerId)
                    .OrderBy(p => p.Id)
                    .Select(p => new
                    {
                        p.Id,
                        productType = (int)p.ProductType,
                        productTypeLabel = p.ProductType.ToString(),
                        p.FurnitureDefinitionEntityId,
                        furnitureName = p.FurnitureDefinition != null
                            ? p.FurnitureDefinition.Name
                            : null,
                        furnitureSpriteId = p.FurnitureDefinition != null
                            ? (int?)p.FurnitureDefinition.SpriteId
                            : null,
                        p.ExtraParam,
                        p.Quantity,
                        p.UniqueSize,
                        p.UniqueRemaining,
                        p.BuildersClubEligible,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // BuildFurniIconUrl isn't translatable to SQL, so the icon URL is attached in a
                // second pass over the already-materialized rows (same two-step shape as
                // FurnitureDefinitionsAsync).
                var products = productRows
                    .Select(p => new
                    {
                        p.Id,
                        p.productType,
                        p.productTypeLabel,
                        p.FurnitureDefinitionEntityId,
                        p.furnitureName,
                        p.furnitureSpriteId,
                        furnitureIconUrl = p.furnitureName is null
                            ? null
                            : BuildFurniIconUrl(p.furnitureName),
                        p.ExtraParam,
                        p.Quantity,
                        p.UniqueSize,
                        p.UniqueRemaining,
                        p.BuildersClubEligible,
                    })
                    .ToList();

                return new
                {
                    offer.Id,
                    offer,
                    products,
                };
            },
            ct
        );

    public Task<object> CatalogCurrencyTypesAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var rows = await db
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

                return new { count = rows.Count, items = rows };
            },
            ct
        );

    /// <summary>
    /// Exposes the raw <c>{id}</c> URL template so the icon picker can build candidate URLs
    /// client-side and probe them via normal &lt;img&gt; load/error events -- there is no manifest
    /// of which icon ids actually exist on the asset host, so "does this id have a real icon" can
    /// only be answered by letting the browser try to load it.
    /// </summary>
    public object CatalogIconTemplate() =>
        new
        {
            template = string.IsNullOrWhiteSpace(_config.CatalogIconUrlTemplate)
                ? null
                : _config.CatalogIconUrlTemplate,
        };

    private string? BuildCatalogIconUrl(int iconId)
    {
        string template = _config.CatalogIconUrlTemplate;

        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        return template.Replace(
            "{id}",
            iconId.ToString(CultureInfo.InvariantCulture),
            StringComparison.Ordinal
        );
    }
}
