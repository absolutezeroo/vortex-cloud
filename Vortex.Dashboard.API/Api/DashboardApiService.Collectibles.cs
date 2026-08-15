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

                // Minting: what may be converted into a Relic, and what stamps cost. A type naming
                // furniture that does not exist is dropped by the grain rather than listed — the
                // client would count the player's copies by a sprite id it has no definition for —
                // so the unresolved flag here is the only warning an admin gets.
                var mintableRows = await db
                    .NftMintableItemTypes.AsNoTracking()
                    .OrderBy(t => t.SortOrder)
                    .ThenBy(t => t.Id)
                    .Select(t => new
                    {
                        t.Id,
                        t.ProductCode,
                        t.StampPrice,
                        t.StartsAt,
                        t.EndsAt,
                        t.RegionLocked,
                        t.LimitedEdition,
                        t.EditionSize,
                        t.Enabled,
                        t.SortOrder,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<string> mintableCodes = mintableRows
                    .Select(t => t.ProductCode)
                    .Distinct()
                    .ToList();

                HashSet<string> knownMintableFurniture = new(
                    await db
                        .FurnitureDefinitions.AsNoTracking()
                        .Where(f => mintableCodes.Contains(f.Name))
                        .Select(f => f.Name)
                        .ToListAsync(ct)
                        .ConfigureAwait(false),
                    StringComparer.Ordinal
                );

                Dictionary<string, int> mintedByCode = await db
                    .NftAssets.AsNoTracking()
                    .Where(a => a.DeletedAt == null && mintableCodes.Contains(a.ProductCode))
                    .GroupBy(a => a.ProductCode)
                    .Select(g => new { ProductCode = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(r => r.ProductCode, r => r.Count, ct)
                    .ConfigureAwait(false);

                DateTime now = DateTime.UtcNow;

                var mintableTypes = mintableRows
                    .Select(t => new
                    {
                        t.Id,
                        t.ProductCode,
                        t.StampPrice,
                        t.StartsAt,
                        t.EndsAt,
                        t.RegionLocked,
                        t.LimitedEdition,
                        t.EditionSize,
                        t.Enabled,
                        t.SortOrder,
                        // How much of a limited edition is gone. Counted from the Relics that
                        // exist, which is also what the mint checks against.
                        mintedCount = mintedByCode.GetValueOrDefault(t.ProductCode),
                        exhausted = t.EditionSize > 0
                            && mintedByCode.GetValueOrDefault(t.ProductCode) >= t.EditionSize,
                        resolved = knownMintableFurniture.Contains(t.ProductCode),
                        // What the player sees: a type is only convertible while its window is
                        // open, and the client gives no reason for a closed one.
                        open = t.Enabled && t.StartsAt <= now && t.EndsAt > now,
                        expired = t.EndsAt <= now,
                        isNft = IsCollectibleClassname(t.ProductCode),
                        iconUrl = BuildFurniIconUrl(t.ProductCode),
                    })
                    .ToList();

                var tokenOffers = await db
                    .NftMintTokenOffers.AsNoTracking()
                    .OrderBy(o => o.SortOrder)
                    .ThenBy(o => o.Id)
                    .Select(o => new
                    {
                        o.Id,
                        o.ProductCode,
                        o.SilverPrice,
                        o.AmountTokens,
                        o.Enabled,
                        o.SortOrder,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var assetRows = await db
                    .NftAssets.AsNoTracking()
                    .Where(a => a.DeletedAt == null)
                    .OrderByDescending(a => a.Id)
                    .Take(50)
                    .Select(a => new
                    {
                        a.Id,
                        a.PlayerEntityId,
                        a.ProductCode,
                        a.StampCost,
                        a.SerialNumber,
                        a.EditionSize,
                        a.CreatedAt,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> assetIds = assetRows.Select(a => a.Id).ToList();

                // Where each of them came from. This is the part of a chain worth keeping: an
                // admin can answer "who had this before" without one.
                var ledgerRows = await db
                    .NftAssetLedger.AsNoTracking()
                    .Where(l => assetIds.Contains(l.NftAssetEntityId))
                    .OrderByDescending(l => l.Id)
                    .Select(l => new
                    {
                        l.Id,
                        assetId = l.NftAssetEntityId,
                        l.FromPlayerEntityId,
                        l.ToPlayerEntityId,
                        l.Reason,
                        l.CreatedAt,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> minterNames = await LoadPlayerNamesAsync(
                        db,
                        NormalizeIds(
                            assetRows
                                .Select(a => (int?)a.PlayerEntityId)
                                .Concat(ledgerRows.Select(l => l.FromPlayerEntityId))
                                .Concat(ledgerRows.Select(l => (int?)l.ToPlayerEntityId))
                        ),
                        ct
                    )
                    .ConfigureAwait(false);

                var assets = assetRows
                    .Select(a => new
                    {
                        a.Id,
                        playerId = a.PlayerEntityId,
                        playerName = ResolvePlayerName(minterNames, a.PlayerEntityId),
                        a.ProductCode,
                        a.StampCost,
                        a.SerialNumber,
                        a.EditionSize,
                        mintedAt = a.CreatedAt,
                        iconUrl = BuildFurniIconUrl(a.ProductCode),
                        history = ledgerRows
                            .Where(l => l.assetId == a.Id)
                            .Select(l => new
                            {
                                l.Id,
                                fromPlayer = l.FromPlayerEntityId is null
                                    ? null
                                    : ResolvePlayerName(minterNames, l.FromPlayerEntityId.Value),
                                toPlayer = ResolvePlayerName(minterNames, l.ToPlayerEntityId),
                                l.Reason,
                                at = l.CreatedAt,
                            })
                            .ToList(),
                    })
                    .ToList();

                int mintedTotal = await db
                    .NftAssets.AsNoTracking()
                    .CountAsync(a => a.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                int stampsHeld = await db
                    .PlayerMintTokens.AsNoTracking()
                    .Where(row => row.DeletedAt == null)
                    .SumAsync(row => row.Balance, ct)
                    .ConfigureAwait(false);

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
                        mintableTypes = mintableTypes.Count,
                        mintableTypesOpen = mintableTypes.Count(t => t.open && t.resolved),
                        mintedRelics = mintedTotal,
                        stampsHeld,
                    },
                    collections = collectionItems,
                    storeOffers,
                    mintableTypes,
                    tokenOffers,
                    assets,
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
