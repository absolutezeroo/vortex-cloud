using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// The four fishing content tables plus what players have done with them, read for the page that
/// edits them. The writes live in <c>DashboardOperationsService.Fishing.cs</c>.
/// </summary>
/// <remarks>
/// Zones carry their species count, because a zone with none is a spot that can be fished and never
/// yields anything — the one misconfiguration here that looks exactly like a bug from the outside.
/// </remarks>
internal sealed partial class DashboardApiService
{
    public Task<object> FishingContentAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var zoneRows = await db
                    .FishingZones.AsNoTracking()
                    .OrderBy(z => z.RequiredLevel)
                    .ThenBy(z => z.Id)
                    .Select(z => new
                    {
                        z.Id,
                        z.NameKey,
                        z.FurniClass,
                        z.RequiredLevel,
                        z.MinCatches,
                        z.MaxCatches,
                        speciesCount = db.FishingSpecies.Count(s => s.ZoneId == z.Id),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // The spot's artwork, so the operator recognises the furni instead of reading a
                // classname. BuildFurniIconUrl does not translate to SQL, so it is attached after.
                var zones = zoneRows
                    .Select(z => new
                    {
                        z.Id,
                        z.NameKey,
                        z.FurniClass,
                        furniIconUrl = BuildFurniIconUrl(z.FurniClass),
                        z.RequiredLevel,
                        z.MinCatches,
                        z.MaxCatches,
                        z.speciesCount,
                    })
                    .ToList();

                var species = await db
                    .FishingSpecies.AsNoTracking()
                    .OrderBy(s => s.ZoneId)
                    .ThenBy(s => s.RequiredLevel)
                    .ThenBy(s => s.Id)
                    .Select(s => new
                    {
                        s.Id,
                        s.ZoneId,
                        s.NameKey,
                        s.RequiredLevel,
                        s.RarityStars,
                        s.CatchRate,
                        s.RarityWeight,
                        s.MinWeight,
                        s.MaxWeight,
                        s.XpReward,
                        s.GoldenXpBonus,
                        s.CurrencyReward,
                        s.ActiveHours,
                        s.ActiveWeekdays,
                        s.ActiveSeasons,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var rodTiers = await db
                    .FishingRodTiers.AsNoTracking()
                    .OrderBy(t => t.Quality)
                    .Select(t => new
                    {
                        t.Id,
                        t.Quality,
                        t.XpThreshold,
                        t.NameKey,
                        t.HandItemId,
                        t.CatchMultiplier,
                        t.GoldenMultiplier,
                        t.HookHavocChance,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var levels = await db
                    .FishingLevels.AsNoTracking()
                    .OrderBy(l => l.Level)
                    .Select(l => new
                    {
                        l.Id,
                        l.Level,
                        l.XpThreshold,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // The share of the draw each species really has: it competes with the others in its
                // own zone, so the denominator is per zone rather than global. Computed here because
                // a page that worked it out itself would be a second implementation of the rule the
                // server draws by.
                var zoneWeights = species
                    .GroupBy(s => s.ZoneId)
                    .ToDictionary(g => g.Key, g => g.Sum(s => (long)s.RarityWeight));

                return new
                {
                    zones,
                    species = species.Select(s => new
                    {
                        s.Id,
                        s.ZoneId,
                        s.NameKey,
                        s.RequiredLevel,
                        s.RarityStars,
                        s.CatchRate,
                        catchRatePercent = Math.Round(s.CatchRate / 10.0, 1),
                        s.RarityWeight,
                        drawSharePercent = zoneWeights.GetValueOrDefault(s.ZoneId) > 0
                            ? Math.Round(s.RarityWeight * 100.0 / zoneWeights[s.ZoneId], 1)
                            : 0,
                        s.MinWeight,
                        s.MaxWeight,
                        s.XpReward,
                        s.GoldenXpBonus,
                        s.CurrencyReward,
                        s.ActiveHours,
                        s.ActiveWeekdays,
                        s.ActiveSeasons,
                        allHours = s.ActiveHours == 0xFFFFFF,
                        allWeekdays = s.ActiveWeekdays == 0b1111111,
                    }),
                    rodTiers,
                    levels,
                };
            },
            ct
        );

    /// <summary>
    /// What players have actually caught: the records board and the derbies. Read-only — a record is
    /// something that happened, not something an operator sets.
    /// </summary>
    public Task<object> FishingActivityAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                int limit = Math.Clamp(ParseInt(query["limit"], 25), 1, 100);

                var records = await db
                    .FishingRecords.AsNoTracking()
                    .OrderByDescending(r => r.BestWeight)
                    .Take(limit)
                    .Select(r => new
                    {
                        r.Id,
                        r.PlayerId,
                        playerName = db
                            .Players.Where(p => p.Id == r.PlayerId)
                            .Select(p => p.Name)
                            .FirstOrDefault(),
                        r.SpeciesId,
                        speciesNameKey = db
                            .FishingSpecies.Where(s => s.Id == r.SpeciesId)
                            .Select(s => s.NameKey)
                            .FirstOrDefault(),
                        r.BestWeight,
                        r.CaughtCount,
                        r.BestAt,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var derbies = await db
                    .FishingDerbies.AsNoTracking()
                    .OrderByDescending(d => d.StartsAt)
                    .Take(limit)
                    .Select(d => new
                    {
                        d.Id,
                        d.NameKey,
                        d.StartsAt,
                        d.EndsAt,
                        entries = db.FishingDerbyEntries.Count(e => e.DerbyId == d.Id),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new
                {
                    records,
                    derbies,
                    anglers = await db.FishingPlayerState.CountAsync(ct).ConfigureAwait(false),
                };
            },
            ct
        );
}
