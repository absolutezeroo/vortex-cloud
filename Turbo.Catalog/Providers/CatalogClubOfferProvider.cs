using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Providers;

namespace Turbo.Catalog.Providers;

public sealed class CatalogClubOfferProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<ICatalogClubOfferProvider> logger
) : ICatalogClubOfferProvider
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<ICatalogClubOfferProvider> _logger = logger;

    private IReadOnlyList<ClubOffer> _offers = [];

    public IReadOnlyList<ClubOffer> GetAll() => _offers;

    public ClubOffer? FindById(int offerId)
    {
        foreach (var offer in _offers)
            if (offer.OfferId == offerId)
                return offer;
        return null;
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var entities = await dbCtx.CatalogClubOffers
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _offers = entities.Select(e => new ClubOffer
        {
            OfferId = e.Id,
            ProductCode = e.ProductCode,
            PriceCredits = e.PriceCredits,
            PriceActivityPoints = e.PriceActivityPoints,
            PriceActivityPointType = e.PriceActivityPointType,
            IsVip = e.IsVip,
            Months = e.Months,
            ExtraDays = e.ExtraDays,
            IsGiftable = e.IsGiftable,
            DaysLeftAfterPurchase = 0,
            Year = 0,
            Month = 0,
            Day = 0,
        }).ToList();

        _logger.LogInformation("Loaded {Count} HC club offers from database", _offers.Count);
    }
}
