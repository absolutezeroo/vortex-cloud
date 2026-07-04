using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Marketplace;
using Turbo.Primitives.Marketplace.Grains;
using Turbo.Primitives.Marketplace.Snapshots;

namespace Turbo.Marketplace.Grains;

public sealed class MarketplaceSearchGrain(IDbContextFactory<TurboDbContext> dbCtxFactory)
    : Grain,
        IMarketplaceSearchGrain
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;

    public async Task<(List<MarketplaceOfferSnapshot> Offers, int TotalFound)> GetOffersAsync(
        int minPrice,
        int maxPrice,
        string searchQuery,
        int sortOrder,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;

        IQueryable<MarketplaceOfferEntity> query = dbCtx
            .MarketplaceOffers.AsNoTracking()
            .Include(o => o.FurnitureDefinitionEntity)
            .Where(o =>
                o.State == MarketplaceOfferState.Active
                && o.ExpiresAt > now
                && o.Price >= minPrice
                && (maxPrice <= 0 || o.Price <= maxPrice)
            );

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(o =>
                o.FurnitureDefinitionEntity != null
                && o.FurnitureDefinitionEntity.Name.Contains(searchQuery)
            );
        }

        List<MarketplaceOfferEntity> raw = await query.ToListAsync(ct).ConfigureAwait(false);

        int totalFound = raw.Count;

        IEnumerable<MarketplaceOfferSnapshot> grouped = raw.GroupBy(o => o.SpriteId)
            .Select(g =>
            {
                int avgPrice = (int)g.Average(o => o.Price);
                int offerCount = g.Count();
                MarketplaceOfferEntity cheapest = g.OrderBy(o => o.Price).First();

                return new MarketplaceOfferSnapshot
                {
                    OfferId = cheapest.Id,
                    SpriteId = cheapest.SpriteId,
                    FurnitureType = cheapest.FurnitureType,
                    ExtraData = cheapest.ExtraData,
                    Price = cheapest.Price,
                    AvgPrice = avgPrice,
                    OfferCount = offerCount,
                    ExpiresIn = (int)Math.Max(0, (cheapest.ExpiresAt - now).TotalSeconds),
                    Status = (int)MarketplaceOfferState.Active,
                    CreditsOwed = 0,
                };
            });

        grouped = sortOrder switch
        {
            1 => grouped.OrderBy(o => o.Price),
            2 => grouped.OrderByDescending(o => o.Price),
            _ => grouped.OrderBy(o => o.SpriteId),
        };

        return (grouped.ToList(), totalFound);
    }

    public async Task<MarketplaceItemStatsSnapshot> GetItemStatsAsync(
        int spriteId,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;

        IQueryable<MarketplaceOfferEntity> matching = dbCtx
            .MarketplaceOffers.AsNoTracking()
            .Where(o =>
                o.SpriteId == spriteId
                && o.State == MarketplaceOfferState.Active
                && o.ExpiresAt > now
            );

        int offerCount = await matching.CountAsync(ct).ConfigureAwait(false);

        if (offerCount == 0)
        {
            return new MarketplaceItemStatsSnapshot
            {
                SpriteId = spriteId,
                AvgPrice = 0,
                OfferCount = 0,
                History = [],
                MinSellValue = 0,
                MaxSellValue = 0,
            };
        }

        int avgPrice = (int)await matching.AverageAsync(o => o.Price, ct).ConfigureAwait(false);
        int minPrice = await matching.MinAsync(o => o.Price, ct).ConfigureAwait(false);
        int maxPrice = await matching.MaxAsync(o => o.Price, ct).ConfigureAwait(false);

        return new MarketplaceItemStatsSnapshot
        {
            SpriteId = spriteId,
            AvgPrice = avgPrice,
            OfferCount = offerCount,
            History = [],
            MinSellValue = minPrice,
            MaxSellValue = maxPrice,
        };
    }
}
