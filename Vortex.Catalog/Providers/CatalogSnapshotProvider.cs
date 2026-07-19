using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Providers;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Furniture.Providers;

namespace Vortex.Catalog.Providers;

public sealed class CatalogSnapshotProvider<TTag>(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<ICatalogSnapshotProvider<TTag>> logger,
    IFurnitureDefinitionProvider furnitureProvider,
    CatalogType catalogType
) : ICatalogSnapshotProvider<TTag>
    where TTag : ICatalogTag
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<ICatalogSnapshotProvider<TTag>> _logger = logger;
    private readonly IFurnitureDefinitionProvider _furnitureProvider = furnitureProvider;
    private CatalogSnapshot _current = CatalogSnapshot.Empty;

    public CatalogType CatalogType => catalogType;
    public CatalogSnapshot Current => _current;

    public async Task<CatalogSnapshot> GetSnapshotAsync(CancellationToken ct)
    {
        if (Current == CatalogSnapshot.Empty)
        {
            await ReloadAsync(ct).ConfigureAwait(false);
        }

        return Current;
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            // catalog_pages/catalog_offers/catalog_products are shared across every CatalogType --
            // this filter is the ONLY place that boundary is enforced. Once a CatalogSnapshot is
            // built for one type, it structurally cannot contain another type's rows: there is no
            // unfiltered snapshot for a handler to accidentally read from.
            List<CatalogPageEntity> pages = await dbCtx
                .CatalogPages.AsNoTracking()
                .Where(p => p.CatalogType == catalogType)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            HashSet<int> pageIds = pages.Select(p => p.Id).ToHashSet();

            List<CatalogOfferEntity> offers = await dbCtx
                .CatalogOffers.AsNoTracking()
                .Where(o => pageIds.Contains(o.CatalogPageEntityId))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            HashSet<int> offerIds = offers.Select(o => o.Id).ToHashSet();

            List<CatalogProductEntity> products = await dbCtx
                .CatalogProducts.AsNoTracking()
                .Where(p => offerIds.Contains(p.CatalogOfferEntityId))
                .ToListAsync(ct)
                .ConfigureAwait(false);
            List<CatalogFrontPageItemEntity> frontPageItems = await dbCtx
                .CatalogFrontPageItems.AsNoTracking()
                .OrderBy(x => x.Position)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            ImmutableDictionary<int, ImmutableArray<int>> pageChildrenIds = pages
                .GroupBy(p => p.ParentEntityId ?? -1)
                .ToImmutableDictionary(
                    g => g.Key,
                    g =>
                        g.OrderBy(x => x.SortOrder)
                            .ThenBy(x => x.Localization)
                            .Select(x => x.Id)
                            .ToImmutableArray()
                );

            ImmutableDictionary<int, ImmutableArray<int>> pageOfferIds = offers
                .GroupBy(o => o.CatalogPageEntityId)
                .ToImmutableDictionary(g => g.Key, g => g.Select(x => x.Id).ToImmutableArray());

            ImmutableDictionary<int, ImmutableArray<int>> offerProductIds = products
                .GroupBy(op => op.CatalogOfferEntityId)
                .ToImmutableDictionary(g => g.Key, g => g.Select(x => x.Id).ToImmutableArray());

            ImmutableDictionary<int, CatalogProductSnapshot> productsById = products
                .Select(x => new CatalogProductSnapshot
                {
                    Id = x.Id,
                    OfferId = x.CatalogOfferEntityId,
                    ProductType = x.ProductType,
                    FurniDefinitionId = x.FurnitureDefinitionEntityId ?? -1,
                    SpriteId =
                        x.FurnitureDefinitionEntityId != null
                            ? _furnitureProvider
                                .TryGetDefinition(x.FurnitureDefinitionEntityId.Value)
                                ?.SpriteId
                                ?? -1
                            : -1,
                    ExtraParam = x.ExtraParam,
                    Quantity = x.Quantity,
                    UniqueSize = x.UniqueSize,
                    UniqueRemaining = x.UniqueRemaining,
                    BuildersClubEligible = x.BuildersClubEligible,
                })
                .ToImmutableDictionary(x => x.Id);

            ImmutableDictionary<int, CatalogOfferSnapshot> offersById = offers
                .Select(x =>
                {
                    ImmutableArray<int> ids = offerProductIds.TryGetValue(
                        x.Id,
                        out ImmutableArray<int> productIds
                    )
                        ? productIds
                        : [];
                    ImmutableArray<CatalogProductSnapshot> products = ids.Select(x =>
                            productsById[x]
                        )
                        .ToImmutableArray();

                    return new CatalogOfferSnapshot()
                    {
                        Id = x.Id,
                        PageId = x.CatalogPageEntityId,
                        LocalizationId = x.LocalizationId ?? string.Empty,
                        Rentable = false,
                        CostCredits = x.CostCredits,
                        CostCurrency = x.CostCurrency,
                        CurrencyTypeId = x.CurrencyTypeId,
                        CostSilver = 0,
                        CanGift = x.CanGift,
                        CanBundle = x.CanBundle,
                        ClubLevel = x.ClubLevel,
                        Visible = x.Visible,
                        ProductIds = ids,
                        Products = products,
                        DiscountPercent = x.DiscountPercent,
                    };
                })
                .ToImmutableDictionary(x => x.Id);

            ImmutableArray<int> rootChildIds = pageChildrenIds.TryGetValue(
                -1,
                out ImmutableArray<int> rootChildren
            )
                ? rootChildren
                : ImmutableArray<int>.Empty;

            CatalogPageSnapshot virtualRoot = new CatalogPageSnapshot
            {
                Id = -1,
                ParentId = -1,
                Localization = string.Empty,
                Name = string.Empty,
                Icon = 0,
                Layout = CatalogPageLayout.Default3x3,
                ImageData = [],
                TextData = [],
                Visible = false,
                OfferIds = [],
                ChildIds = rootChildIds,
            };

            ImmutableDictionary<int, CatalogPageSnapshot> pagesById = pages
                .Select(x => new CatalogPageSnapshot
                {
                    Id = x.Id,
                    ParentId = x.ParentEntityId ?? -1,
                    Localization = x.Localization,
                    Name = x.Name,
                    Icon = x.Icon,
                    Layout = x.Layout,
                    ImageData = x.ImageData ?? [],
                    TextData = x.TextData ?? [],
                    Visible = x.Visible,
                    OfferIds = pageOfferIds.TryGetValue(x.Id, out ImmutableArray<int> offerIds)
                        ? offerIds
                        : [],
                    ChildIds = pageChildrenIds.TryGetValue(x.Id, out ImmutableArray<int> childIds)
                        ? childIds
                        : [],
                })
                .Prepend(virtualRoot)
                .ToImmutableDictionary(x => x.Id);

            ImmutableArray<CatalogFrontPageItemSnapshot> frontPageItemSnapshots = frontPageItems
                .Select(x => new CatalogFrontPageItemSnapshot
                {
                    Position = x.Position,
                    ItemName = x.ItemName,
                    ItemPromoImage = x.ItemPromoImage,
                    Type = (CatalogFrontPageItemType)x.Type,
                    CatalogPageLocation = x.CatalogPageLocation,
                    ProductOfferId = x.ProductOfferId,
                    ProductCode = x.ProductCode,
                    ExpiresInSeconds = x.ExpiresInSeconds,
                })
                .ToImmutableArray();

            CatalogSnapshot snapshot = new CatalogSnapshot
            {
                CatalogType = CatalogType,
                RootPageId = -1,
                PagesById = pagesById,
                OffersById = offersById,
                ProductsById = productsById,
                PageChildrenIds = pageChildrenIds,
                PageOfferIds = pageOfferIds,
                OfferProductIds = offerProductIds,
                FrontPageItems = frontPageItemSnapshots,
            };

            _logger.LogInformation(
                "Loaded catalog snapshot: Type={CatalogType}, TotalPages={TotalPageCount}, Offers={TotalOfferCount}, Products={TotalProductCount}",
                snapshot.CatalogType,
                snapshot.PagesById.Count,
                snapshot.OffersById.Count,
                snapshot.ProductsById.Count
            );

            Volatile.Write(ref _current, snapshot);
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
