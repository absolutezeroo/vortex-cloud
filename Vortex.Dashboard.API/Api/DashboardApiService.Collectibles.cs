using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// NFT collections, the items that make them up, and the collector scores players have reached.
/// <para>
/// A collection is only completable if the furniture its items name actually exists in the
/// catalogue, so each item is matched against <c>furniture_definitions</c> by classname — an item
/// pointing at nothing is a collection no player can ever finish, and no other surface would show
/// it.
/// </para>
/// </summary>
internal sealed partial class DashboardApiService
{
    public Task<object> CollectiblesAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var collections = await db
                    .NftCollections.AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.CollectionCode,
                        c.Name,
                        c.BoostScore,
                        c.ReleasedAt,
                        c.SnapshotAt,
                        c.Status,
                        c.RewardProductCode,
                        c.BonusProductCode,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var items = await db
                    .NftCollectionItems.AsNoTracking()
                    .OrderBy(i => i.NftCollectionEntityId)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => new
                    {
                        i.Id,
                        collectionId = i.NftCollectionEntityId,
                        i.ProductCode,
                        i.ItemTypeId,
                        i.ProductTypeId,
                        i.Score,
                        i.Rarity,
                        i.SortOrder,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<string> productCodes = items.Select(i => i.ProductCode).Distinct().ToList();

                // Matching by classname is what the collection's completion check has to do too: the
                // rows carry a product code, not a definition id.
                HashSet<string> knownFurniture = new(
                    await db
                        .FurnitureDefinitions.AsNoTracking()
                        .Where(f => productCodes.Contains(f.Name))
                        .Select(f => f.Name)
                        .ToListAsync(ct)
                        .ConfigureAwait(false),
                    StringComparer.Ordinal
                );

                var collectionItems = collections
                    .Select(c =>
                    {
                        var owned = items.Where(i => i.collectionId == c.Id).ToList();
                        int unresolved = owned.Count(i => !knownFurniture.Contains(i.ProductCode));

                        return new
                        {
                            c.Id,
                            c.CollectionCode,
                            c.Name,
                            c.BoostScore,
                            c.ReleasedAt,
                            c.SnapshotAt,
                            c.Status,
                            c.RewardProductCode,
                            c.BonusProductCode,
                            itemCount = owned.Count,
                            totalScore = owned.Sum(i => i.Score),
                            unresolvedItems = unresolved,
                            completable = owned.Count > 0 && unresolved == 0,
                            items = owned
                                .Select(i => new
                                {
                                    i.Id,
                                    i.ProductCode,
                                    i.ItemTypeId,
                                    i.ProductTypeId,
                                    i.Score,
                                    i.Rarity,
                                    i.SortOrder,
                                    resolved = knownFurniture.Contains(i.ProductCode),
                                    iconUrl = BuildFurniIconUrl(i.ProductCode),
                                })
                                .ToList(),
                        };
                    })
                    .ToList();

                var collectorRows = await db
                    .PlayerCollectorStats.AsNoTracking()
                    .OrderByDescending(s => s.HighestScore)
                    .Take(15)
                    .Select(s => new { s.PlayerEntityId, s.HighestScore })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> names = await LoadPlayerNamesAsync(
                        db,
                        NormalizeIds(collectorRows.Select(c => (int?)c.PlayerEntityId)),
                        ct
                    )
                    .ConfigureAwait(false);

                int trackedPlayers = await db
                    .PlayerCollectorStats.AsNoTracking()
                    .CountAsync(ct)
                    .ConfigureAwait(false);

                return new
                {
                    totals = new
                    {
                        collections = collectionItems.Count,
                        items = items.Count,
                        unresolvedItems = collectionItems.Sum(c => c.unresolvedItems),
                        completableCollections = collectionItems.Count(c => c.completable),
                        trackedPlayers,
                    },
                    collections = collectionItems,
                    topCollectors = collectorRows
                        .Select(c => new
                        {
                            playerId = c.PlayerEntityId,
                            playerName = ResolvePlayerName(names, c.PlayerEntityId),
                            score = c.HighestScore,
                        })
                        .ToList(),
                };
            },
            ct
        );
}
