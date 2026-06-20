using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Catalog;
using Turbo.Primitives.Catalog.Providers;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Furniture.Enums;
using Turbo.Primitives.Furniture.Providers;

namespace Turbo.Catalog.Providers;

public sealed class CatalogClubGiftProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<ICatalogClubGiftProvider> logger,
    IFurnitureDefinitionProvider furnitureProvider
) : ICatalogClubGiftProvider
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<ICatalogClubGiftProvider> _logger = logger;
    private readonly IFurnitureDefinitionProvider _furnitureProvider = furnitureProvider;

    private IReadOnlyList<CatalogOfferSnapshot> _offers = [];

    public IReadOnlyList<CatalogOfferSnapshot> GetAll() => _offers;

    public CatalogOfferSnapshot? FindByProductCode(string productCode) =>
        _offers.FirstOrDefault(o => o.LocalizationId == productCode);

    public async Task ReloadAsync(CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<CatalogClubGiftEntity> entities = await dbCtx
            .CatalogClubGifts.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _offers = entities.Select((e, i) => BuildSnapshot(e, i)).ToList();

        _logger.LogInformation("Loaded {Count} HC club gift offers from database", _offers.Count);
    }

    private CatalogOfferSnapshot BuildSnapshot(
        Database.Entities.Catalog.CatalogClubGiftEntity e,
        int index
    )
    {
        ProductType productType = e.ProductType.FromLegacyString();

        int spriteId =
            productType is not ProductType.Badge && e.FurniDefinitionId.HasValue
                ? _furnitureProvider.TryGetDefinition(e.FurniDefinitionId.Value)?.SpriteId ?? -1
                : -1;

        CatalogProductSnapshot product = new CatalogProductSnapshot
        {
            Id = index,
            OfferId = e.Id,
            ProductType = productType,
            FurniDefinitionId = e.FurniDefinitionId ?? -1,
            SpriteId = spriteId,
            ExtraParam = e.ExtraParam,
            Quantity = e.Quantity,
            UniqueSize = 0,
            UniqueRemaining = 0,
        };

        return new CatalogOfferSnapshot
        {
            Id = e.Id,
            PageId = -1,
            LocalizationId = e.ProductCode,
            Rentable = false,
            CostCredits = 0,
            CostCurrency = 0,
            CurrencyTypeId = null,
            CostSilver = 0,
            CanGift = false,
            CanBundle = false,
            ClubLevel = e.IsVip ? 2 : 1,
            Visible = true,
            ProductIds = [product.Id],
            Products = [product],
        };
    }
}
