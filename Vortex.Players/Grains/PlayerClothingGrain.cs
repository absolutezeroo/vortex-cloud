using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Players.Grains;

/// <summary>
/// One player's wearable figure sets, and turning a clothing furni into them.
/// </summary>
/// <remarks>
/// <para>
/// The furni is consumed, so this runs where a double click cannot become two conversions: the
/// client leaves its confirm button live until an answer arrives, and its only answer is the
/// inventory list coming back.
/// </para>
/// <para>
/// The furniture row is soft-deleted by the same query that checks it is still the player's, so a
/// repeated call finds nothing to delete and grants nothing.
/// </para>
/// </remarks>
internal sealed class PlayerClothingGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    ILogger<PlayerClothingGrain> logger
) : Grain, IPlayerClothingGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<PlayerClothingGrain> _logger = logger;

    private PlayerId PlayerId => new((int)this.GetPrimaryKeyLong());

    public async Task<PlayerClothingSnapshot> GetUnlockedAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        return await ReadSnapshotAsync(dbCtx, ct).ConfigureAwait(true);
    }

    public async Task<ClothingRedeemResult> RedeemAsync(int itemId, CancellationToken ct)
    {
        IInventoryGrain inventory = _grainFactory.GetInventoryGrain(PlayerId);
        RoomObjectId objectId = new(itemId);

        // The inventory only holds furniture that is not standing in a room. Finding it here is
        // what establishes both that the player owns it and that it is theirs to consume.
        FurnitureItemSnapshot? item = await inventory
            .GetItemSnapshotAsync(objectId, ct)
            .ConfigureAwait(true);

        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        if (item is null)
        {
            return await FailAsync(ClothingRedeemOutcome.NotOwned, dbCtx, ct).ConfigureAwait(true);
        }

        int[] granted = await dbCtx
            .FurniturePurchasableClothing.AsNoTracking()
            .Where(row =>
                row.FurnitureDefinitionEntityId == item.Definition.Id && row.DeletedAt == null
            )
            .Select(row => row.FigureSetId)
            .Distinct()
            .ToArrayAsync(ct)
            .ConfigureAwait(true);

        if (granted.Length == 0)
        {
            // Nothing in the mapping names this furni, so redeeming it would consume it and hand
            // over nothing. Refused rather than eaten.
            _logger.LogWarning(
                "Clothing furni {ProductCode} (definition {DefinitionId}) grants no figure set; refusing to consume it for player {PlayerId}.",
                item.Definition.Name,
                item.Definition.Id,
                PlayerId
            );

            return await FailAsync(ClothingRedeemOutcome.GrantsNothing, dbCtx, ct)
                .ConfigureAwait(true);
        }

        string productCode = item.Definition.Name;

        HashSet<int> alreadyHeld =
        [
            .. await dbCtx
                .PlayerClothing.AsNoTracking()
                .Where(row =>
                    row.PlayerEntityId == PlayerId.Value
                    && row.ProductCode == productCode
                    && row.DeletedAt == null
                )
                .Select(row => row.FigureSetId)
                .ToArrayAsync(ct)
                .ConfigureAwait(true),
        ];

        int[] missing = [.. granted.Where(setId => !alreadyHeld.Contains(setId))];

        if (missing.Length == 0)
        {
            // This exact furni is already bound to the account. The client would not normally ask —
            // it sends the look change straight away in that case — so the furni is left alone.
            return await FailAsync(ClothingRedeemOutcome.AlreadyOwned, dbCtx, ct)
                .ConfigureAwait(true);
        }

        // Deleting the furniture row is the step that decides the redemption: scoped to this player
        // and to a row that is not already gone, so a repeat grants nothing.
        // No room condition here, deliberately. A room detaches its furniture from the database in
        // a deferred batch, so a row keeps naming the room it just left for as long as that flush
        // takes -- and requiring room_id to be null refused every item that had been in a room
        // moments earlier, silently. The real guard is the inventory snapshot read above: the
        // inventory only ever holds furniture that is not standing in a room.
        int deleted = await dbCtx
            .Furnitures.Where(furni =>
                furni.Id == itemId
                && furni.PlayerEntityId == PlayerId.Value
                && furni.DeletedAt == null
            )
            .ExecuteUpdateAsync(
                row =>
                    row.SetProperty(furni => furni.DeletedAt, DateTime.UtcNow)
                        .SetProperty(furni => furni.RoomEntityId, (int?)null),
                ct
            )
            .ConfigureAwait(true);

        if (deleted == 0)
        {
            return await FailAsync(ClothingRedeemOutcome.NotOwned, dbCtx, ct).ConfigureAwait(true);
        }

        try
        {
            foreach (int setId in missing)
            {
                dbCtx.PlayerClothing.Add(
                    new PlayerClothingEntity
                    {
                        PlayerEntityId = PlayerId.Value,
                        FigureSetId = setId,
                        ProductCode = productCode,
                    }
                );
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // The furni is already gone and there is no putting that exact item back, so an
            // equivalent one is granted rather than leaving the player with neither.
            _logger.LogError(
                ex,
                "Recording clothing sets for player {PlayerId} failed after {ProductCode} was consumed; granting the furniture back.",
                PlayerId,
                productCode
            );

            await inventory
                .GrantFurnitureDefinitionAsync(item.Definition.Id, null, ct)
                .ConfigureAwait(true);

            return await FailAsync(ClothingRedeemOutcome.Failed, dbCtx, ct).ConfigureAwait(true);
        }

        // Only now does the client hear about it: the item leaves the inventory view.
        await inventory.RemoveFurnitureAsync(objectId, ct).ConfigureAwait(true);

        _logger.LogInformation(
            "Player {PlayerId} redeemed {ProductCode} for figure set(s) {Sets}.",
            PlayerId,
            productCode,
            string.Join(", ", missing)
        );

        return new ClothingRedeemResult
        {
            Outcome = ClothingRedeemOutcome.Redeemed,
            Clothing = await ReadSnapshotAsync(dbCtx, ct).ConfigureAwait(true),
        };
    }

    public async Task<ImmutableArray<int>> FindUnownedSellableAsync(
        ImmutableArray<int> figureSetIds,
        CancellationToken ct
    )
    {
        if (figureSetIds.IsDefaultOrEmpty)
        {
            return [];
        }

        int[] worn = [.. figureSetIds];

        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        return
        [
            .. await dbCtx
                .FigureSellableSets.AsNoTracking()
                .Where(sellable =>
                    worn.Contains(sellable.FigureSetId)
                    && sellable.DeletedAt == null
                    && !dbCtx.PlayerClothing.Any(owned =>
                        owned.PlayerEntityId == PlayerId.Value
                        && owned.FigureSetId == sellable.FigureSetId
                        && owned.DeletedAt == null
                    )
                )
                .Select(sellable => sellable.FigureSetId)
                .ToArrayAsync(ct)
                .ConfigureAwait(true),
        ];
    }

    /// <summary>
    /// The lists are re-read even on a refusal: the client's only acknowledgement is receiving them,
    /// so answering with the truth costs one query and avoids leaving it waiting out its five-second
    /// window for nothing.
    /// </summary>
    private async Task<ClothingRedeemResult> FailAsync(
        ClothingRedeemOutcome outcome,
        VortexDbContext dbCtx,
        CancellationToken ct
    ) =>
        new()
        {
            Outcome = outcome,
            Clothing = await ReadSnapshotAsync(dbCtx, ct).ConfigureAwait(true),
        };

    private async Task<PlayerClothingSnapshot> ReadSnapshotAsync(
        VortexDbContext dbCtx,
        CancellationToken ct
    )
    {
        var rows = await dbCtx
            .PlayerClothing.AsNoTracking()
            .Where(row => row.PlayerEntityId == PlayerId.Value && row.DeletedAt == null)
            .Select(row => new { row.FigureSetId, row.ProductCode })
            .ToArrayAsync(ct)
            .ConfigureAwait(true);

        return new PlayerClothingSnapshot
        {
            FigureSetIds = [.. rows.Select(row => row.FigureSetId).Distinct().Order()],
            BoundFurnitureNames =
            [
                .. rows.Select(row => row.ProductCode)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal),
            ],
        };
    }
}
