using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;

namespace Vortex.Dashboard.API.Api;

internal sealed partial class DashboardApiService
{
    /// <summary>Read-only overview of the pets domain: population, type/race/rarity distribution,
    /// breeding activity, and average health (energy/nutrition). There is no dedicated pet audit
    /// category today, so this reads straight off <c>PetEntity</c> rather than an audit trail.</summary>
    public Task<object> PetsStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime until = ParseDateTime(query["until"]) ?? DateTime.UtcNow;
                DateTime since = ParseDateTime(query["since"]) ?? until.AddDays(-30);
                string granularity = NormalizeGranularity(query["granularity"]);

                List<PetStatsRow> rows = await db
                    .Pets.AsNoTracking()
                    .Select(p => new PetStatsRow(
                        p.CreatedAt,
                        p.Type,
                        p.Race,
                        p.Level,
                        p.Energy,
                        p.Nutrition,
                        p.RarityLevel,
                        p.CanBreed,
                        p.ParentOneId,
                        p.ParentTwoId,
                        p.OwnerPlayerEntityId
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                int totalPets = rows.Count;
                double avgLevel = totalPets > 0 ? Math.Round(rows.Average(r => r.Level), 2) : 0d;
                double avgEnergy = totalPets > 0 ? Math.Round(rows.Average(r => r.Energy), 2) : 0d;
                double avgNutrition =
                    totalPets > 0 ? Math.Round(rows.Average(r => r.Nutrition), 2) : 0d;
                int breedablePets = rows.Count(r => r.CanBreed);
                int bredPets = rows.Count(r =>
                    r.ParentOneId is not null || r.ParentTwoId is not null
                );

                var byType = rows.GroupBy(r => r.Type)
                    .Select(g => new { type = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .ToList();

                var byRace = rows.GroupBy(r => new { r.Type, r.Race })
                    .Select(g => new
                    {
                        type = g.Key.Type,
                        race = g.Key.Race,
                        count = g.Count(),
                    })
                    .OrderByDescending(g => g.count)
                    .Take(20)
                    .ToList();

                var byRarity = rows.GroupBy(r => r.RarityLevel)
                    .Select(g => new { rarityLevel = g.Key, count = g.Count() })
                    .OrderBy(g => g.rarityLevel)
                    .ToList();

                Dictionary<DateTime, int> bucketMap = new();
                DateTime cursor = ResolveCalendarBucket(since, granularity);
                DateTime end = ResolveCalendarBucket(until, granularity);

                while (cursor <= end)
                {
                    bucketMap[cursor] = 0;
                    cursor = NextCalendarBucket(cursor, granularity);
                }

                foreach (
                    PetStatsRow row in rows.Where(r => r.CreatedAt >= since && r.CreatedAt <= until)
                )
                {
                    DateTime bucket = ResolveCalendarBucket(row.CreatedAt, granularity);
                    bucketMap[bucket] = bucketMap.GetValueOrDefault(bucket) + 1;
                }

                var growth = bucketMap
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new
                    {
                        bucket = pair.Key.ToString("O"),
                        label = FormatCalendarLabel(pair.Key, granularity),
                        petsCreated = pair.Value,
                    })
                    .ToList();

                var topOwners = await db
                    .Pets.AsNoTracking()
                    .GroupBy(p => p.OwnerPlayerEntityId)
                    .Select(g => new { ownerId = g.Key, petCount = g.Count() })
                    .OrderByDescending(g => g.petCount)
                    .Take(10)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> ownerIds = NormalizeIds(topOwners.Select(o => (int?)o.ownerId));
                Dictionary<int, string> ownerNames = await LoadPlayerNamesAsync(db, ownerIds, ct)
                    .ConfigureAwait(false);

                var topOwnersWithNames = topOwners
                    .Select(o => new
                    {
                        o.ownerId,
                        ownerName = ResolvePlayerName(ownerNames, (int?)o.ownerId),
                        o.petCount,
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
                        totalPets,
                        avgLevel,
                        avgEnergy,
                        avgNutrition,
                        breedablePets,
                        bredPets,
                    },
                    byType,
                    byRace,
                    byRarity,
                    growth,
                    topOwners = topOwnersWithNames,
                };
            },
            ct
        );

    private sealed record PetStatsRow(
        DateTime CreatedAt,
        int Type,
        int Race,
        int Level,
        int Energy,
        int Nutrition,
        int RarityLevel,
        bool CanBreed,
        int? ParentOneId,
        int? ParentTwoId,
        int OwnerPlayerEntityId
    );
}
