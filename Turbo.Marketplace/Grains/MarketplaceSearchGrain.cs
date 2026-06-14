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
        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var now = DateTime.UtcNow;

        var query = dbCtx
            .MarketplaceOffers.Include(o => o.FurnitureDefinitionEntity)
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

        var raw = await query.ToListAsync(ct).ConfigureAwait(false);

        var totalFound = raw.Count;

        var grouped = raw.GroupBy(o => o.SpriteId)
            .Select(g =>
            {
                var avgPrice = (int)g.Average(o => o.Price);
                var offerCount = g.Count();
                var cheapest = g.OrderBy(o => o.Price).First();

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
        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var now = DateTime.UtcNow;

        var offers = await dbCtx
            .MarketplaceOffers.Where(o =>
                o.SpriteId == spriteId
                && o.State == MarketplaceOfferState.Active
                && o.ExpiresAt > now
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (offers.Count == 0)
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

        var avgPrice = (int)offers.Average(o => o.Price);
        var minPrice = offers.Min(o => o.Price);
        var maxPrice = offers.Max(o => o.Price);

        return new MarketplaceItemStatsSnapshot
        {
            SpriteId = spriteId,
            AvgPrice = avgPrice,
            OfferCount = offers.Count,
            History = [],
            MinSellValue = minPrice,
            MaxSellValue = maxPrice,
        };
    }
}
