using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Marketplace;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// The corners of the economy that have their own tables but no surface of their own: LTD series and
/// their raffle entries, rentable spaces, the currency catalogue, and the builders-club ladder.
/// <para>
/// Each is small enough that a single read serves the whole page; what matters is the pairing —
/// an LTD series is meaningless without its entry outcomes, and a rentable space is meaningless
/// without whether anyone is renting it.
/// </para>
/// </summary>
internal sealed partial class DashboardApiService
{
    public Task<object> EconomyExtrasAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime now = DateTime.UtcNow;

                var series = await db
                    .LtdSeries.AsNoTracking()
                    .OrderByDescending(s => s.Id)
                    .Select(s => new
                    {
                        s.Id,
                        s.CatalogProductEntityId,
                        s.TotalQuantity,
                        s.RemainingQuantity,
                        s.CostCredits,
                        s.RaffleWindowSeconds,
                        s.IsActive,
                        s.HasRaffleFinished,
                        s.StartsAt,
                        s.EndsAt,
                        s.CreatedAt,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> seriesIds = series.ConvertAll(s => s.Id);

                var entryRows = await db
                    .LtdRaffleEntries.AsNoTracking()
                    .Where(e => seriesIds.Contains(e.SeriesEntityId))
                    .GroupBy(e => new { e.SeriesEntityId, e.Result })
                    .Select(g => new
                    {
                        seriesId = g.Key.SeriesEntityId,
                        result = g.Key.Result,
                        count = g.Count(),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var pendingBySeries = await db
                    .LtdRaffleEntries.AsNoTracking()
                    .Where(e => seriesIds.Contains(e.SeriesEntityId) && e.ProcessedAt == null)
                    .GroupBy(e => e.SeriesEntityId)
                    .Select(g => new { seriesId = g.Key, pending = g.Count() })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, int> pendingCounts = pendingBySeries.ToDictionary(
                    p => p.seriesId,
                    p => p.pending
                );

                // The product row carries the display name and the furniture the series actually
                // hands out; an id on its own tells an operator nothing.
                List<int> productIds = series.ConvertAll(s => s.CatalogProductEntityId);

                Dictionary<int, string> productNames = await db
                    .CatalogProducts.AsNoTracking()
                    .Where(p => productIds.Contains(p.Id))
                    .Join(
                        db.FurnitureDefinitions.AsNoTracking(),
                        product => product.FurnitureDefinitionEntityId,
                        definition => definition.Id,
                        (product, definition) => new { product.Id, definition.Name }
                    )
                    .ToDictionaryAsync(x => x.Id, x => x.Name, ct)
                    .ConfigureAwait(false);

                var ltdItems = series
                    .Select(s => new
                    {
                        s.Id,
                        productId = s.CatalogProductEntityId,
                        productName = productNames.GetValueOrDefault(s.CatalogProductEntityId),
                        iconUrl = productNames.TryGetValue(s.CatalogProductEntityId, out string? n)
                            ? BuildFurniIconUrl(n)
                            : null,
                        s.TotalQuantity,
                        s.RemainingQuantity,
                        sold = s.TotalQuantity - s.RemainingQuantity,
                        s.CostCredits,
                        s.RaffleWindowSeconds,
                        s.IsActive,
                        s.HasRaffleFinished,
                        s.StartsAt,
                        s.EndsAt,
                        running = s.IsActive
                            && !s.HasRaffleFinished
                            && (s.StartsAt == null || s.StartsAt <= now)
                            && (s.EndsAt == null || s.EndsAt > now),
                        pendingEntries = pendingCounts.GetValueOrDefault(s.Id),
                        entriesByResult = entryRows
                            .Where(e => e.seriesId == s.Id)
                            .Select(e => new { e.result, e.count })
                            .ToList(),
                    })
                    .ToList();

                var rentableTerms = await db
                    .RentableSpaceTerms.AsNoTracking()
                    .Select(t => new
                    {
                        t.Id,
                        t.FurnitureEntityId,
                        t.Price,
                        t.CurrencyTypeEntityId,
                        t.RentDurationSeconds,
                        t.RequiresHc,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var rentals = await db
                    .RoomRentableSpaces.AsNoTracking()
                    .Select(r => new
                    {
                        r.Id,
                        r.FurnitureEntityId,
                        r.RenterPlayerEntityId,
                        r.RentedUntil,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // A rentable space is a placed furni; showing "#412" makes the operator go and look
                // the id up somewhere else, so the definition's name and icon come along.
                List<int> rentedFurnitureIds = rentals.ConvertAll(r => r.FurnitureEntityId);

                Dictionary<int, string> rentedFurnitureNames = await db
                    .Furnitures.AsNoTracking()
                    .Where(f => rentedFurnitureIds.Contains(f.Id))
                    .Join(
                        db.FurnitureDefinitions.AsNoTracking(),
                        furniture => furniture.FurnitureDefinitionEntityId,
                        definition => definition.Id,
                        (furniture, definition) => new { furniture.Id, definition.Name }
                    )
                    .ToDictionaryAsync(x => x.Id, x => x.Name, ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> renterNames = await LoadPlayerNamesAsync(
                        db,
                        NormalizeIds(rentals.Select(r => r.RenterPlayerEntityId)),
                        ct
                    )
                    .ConfigureAwait(false);

                Dictionary<int, int> termsByFurniture = rentableTerms
                    .GroupBy(t => t.FurnitureEntityId)
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var rentableItems = rentals
                    .Select(r => new
                    {
                        r.Id,
                        furnitureId = r.FurnitureEntityId,
                        furnitureName = rentedFurnitureNames.GetValueOrDefault(r.FurnitureEntityId),
                        iconUrl = rentedFurnitureNames.TryGetValue(
                            r.FurnitureEntityId,
                            out string? rentedName
                        )
                            ? BuildFurniIconUrl(rentedName)
                            : null,
                        renterId = r.RenterPlayerEntityId,
                        renterName = r.RenterPlayerEntityId is { } renter
                            ? ResolvePlayerName(renterNames, renter)
                            : null,
                        r.RentedUntil,
                        rented = r.RenterPlayerEntityId is not null
                            && r.RentedUntil is { } until
                            && until > now,
                        hasTerms = termsByFurniture.ContainsKey(r.FurnitureEntityId),
                    })
                    .ToList();

                var currencies = await db
                    .CurrencyTypes.AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        currencyType = c.CurrencyType.ToString(),
                        c.ActivityPointType,
                        c.Enabled,
                        c.StartingAmount,
                        walletRows = db.PlayerCurrencies.Count(p => p.CurrencyTypeEntityId == c.Id),
                        totalHeld = db.PlayerCurrencies.Where(p => p.CurrencyTypeEntityId == c.Id)
                            .Sum(p => (long?)p.Amount)
                            ?? 0L,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var buildersClub = await db
                    .BuildersClubTiers.AsNoTracking()
                    .OrderBy(t => t.Level)
                    .Select(t => new
                    {
                        t.Id,
                        t.Level,
                        t.FurniLimit,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                MarketplaceSettingsEntity? marketplaceSettings = await db
                    .MarketplaceSettings.AsNoTracking()
                    .OrderBy(s => s.Id)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                return new
                {
                    totals = new
                    {
                        ltdSeries = ltdItems.Count,
                        runningSeries = ltdItems.Count(s => s.running),
                        rentableSpaces = rentableItems.Count,
                        rentedNow = rentableItems.Count(r => r.rented),
                        rentableTerms = rentableTerms.Count,
                        currencies = currencies.Count,
                        buildersClubTiers = buildersClub.Count,
                    },
                    ltdSeries = ltdItems,
                    rentableSpaces = rentableItems,
                    rentableTerms,
                    currencies,
                    buildersClub,
                    marketplaceSettings,
                };
            },
            ct
        );
}
