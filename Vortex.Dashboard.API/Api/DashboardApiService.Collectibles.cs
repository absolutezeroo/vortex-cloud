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
    /// <summary>
    /// Whether the client will treat this furniture as a collectible at all. It decides from the
    /// classname alone — <c>GroupItem.isNft()</c> is <c>className.indexOf("nft_") == 0</c> — and
    /// uses it both to keep such items out of the ordinary Furni list and to fill the Collectibles
    /// category. A shop offer or a relic naming anything else still works, but the buyer receives
    /// what the client files as plain furniture.
    /// </summary>
    private static bool IsCollectibleClassname(string productCode) =>
        productCode.StartsWith("nft_", StringComparison.Ordinal);

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

                // The shop is checked the same way a collection item is: an offer naming furniture
                // that does not exist would take the buyer's emeralds and hand over nothing, so the
                // grain refuses the sale — and this is the only place that would say so beforehand.
                var offerRows = await db
                    .NftStoreOffers.AsNoTracking()
                    .OrderBy(o => o.SortOrder)
                    .ThenBy(o => o.Id)
                    .Select(o => new
                    {
                        o.Id,
                        o.ProductCode,
                        o.EmeraldPrice,
                        o.IsFeatured,
                        o.IsLimited,
                        o.MintLimit,
                        o.SoldCount,
                        o.ItemTypeId,
                        o.ProductTypeId,
                        o.Score,
                        o.Rarity,
                        o.Enabled,
                        o.SortOrder,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<string> offerCodes = offerRows.Select(o => o.ProductCode).Distinct().ToList();

                HashSet<string> knownOfferFurniture = new(
                    await db
                        .FurnitureDefinitions.AsNoTracking()
                        .Where(f => offerCodes.Contains(f.Name))
                        .Select(f => f.Name)
                        .ToListAsync(ct)
                        .ConfigureAwait(false),
                    StringComparer.Ordinal
                );

                var storeOffers = offerRows
                    .Select(o => new
                    {
                        o.Id,
                        o.ProductCode,
                        o.EmeraldPrice,
                        o.IsFeatured,
                        o.IsLimited,
                        o.MintLimit,
                        o.SoldCount,
                        o.ItemTypeId,
                        o.ProductTypeId,
                        o.Score,
                        o.Rarity,
                        o.Enabled,
                        o.SortOrder,
                        resolved = knownOfferFurniture.Contains(o.ProductCode),
                        soldOut = o.MintLimit > 0 && o.SoldCount >= o.MintLimit,
                        // The client decides what counts as a collectible purely from the
                        // classname: GroupItem.isNft() is className.indexOf("nft_") == 0. Anything
                        // else is hidden from the inventory's Collectibles category and listed as
                        // ordinary furniture, however it was bought.
                        isNft = IsCollectibleClassname(o.ProductCode),
                        iconUrl = BuildFurniIconUrl(o.ProductCode),
                    })
                    .ToList();

                // The Relics waiting to be collected. Only outstanding ones are listed: a claim the
                // player has already taken in full is history, not a reward.
                var claimRows = await db
                    .NftClaims.AsNoTracking()
                    .Where(c => c.ClaimedAmount < c.ClaimLimit)
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.PlayerEntityId,
                        c.ProductCode,
                        c.SetId,
                        c.Collection,
                        c.ClaimLimit,
                        c.ClaimedAmount,
                        c.ValidFrom,
                        c.ValidTo,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> claimNames = await LoadPlayerNamesAsync(
                        db,
                        NormalizeIds(claimRows.Select(c => (int?)c.PlayerEntityId)),
                        ct
                    )
                    .ConfigureAwait(false);

                var claims = claimRows
                    .Select(c => new
                    {
                        c.Id,
                        playerId = c.PlayerEntityId,
                        playerName = ResolvePlayerName(claimNames, c.PlayerEntityId),
                        c.ProductCode,
                        c.SetId,
                        c.Collection,
                        c.ClaimLimit,
                        c.ClaimedAmount,
                        remaining = c.ClaimLimit - c.ClaimedAmount,
                        c.ValidFrom,
                        c.ValidTo,
                        isNft = IsCollectibleClassname(c.ProductCode),
                        iconUrl = BuildFurniIconUrl(c.ProductCode),
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
                        storeOffers = storeOffers.Count,
                        storeOffersOnSale = storeOffers.Count(o =>
                            o.Enabled && !o.soldOut && o.resolved
                        ),
                    },
                    collections = collectionItems,
                    storeOffers,
                    claims,
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
