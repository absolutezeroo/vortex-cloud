using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Wired;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Rooms.Grains;

/// <summary>
/// Wired chests: the furni that hold real value for a room's wiring to hand out.
/// </summary>
public sealed partial class RoomGrain
{
    /// <summary>The classnames that are a chest, and which half they store.</summary>
    private static bool IsCoinChestClass(string className) =>
        className.StartsWith("wf_storage_coins", StringComparison.Ordinal);

    private static bool IsChestClass(string className) =>
        className.StartsWith("wf_storage_", StringComparison.Ordinal);

    public async Task<WiredChestSnapshot?> WithdrawWiredChestCreditsAsync(
        ActionContext ctx,
        int chestId,
        int amount,
        CancellationToken ct
    )
    {
        if (!await CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == chestId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (chest is null || chest.Credits <= 0)
            {
                return null;
            }

            // The button for "everything" asks for it by sending nothing sensible, and a request
            // for more than the chest holds is not an error, it is a request for the rest.
            int taken = amount <= 0 ? chest.Credits : Math.Min(amount, chest.Credits);

            chest.Credits -= taken;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            // The chest is emptied first and put back if the wallet refuses, the same way a
            // purchase refunds itself: money that left a chest and reached nobody is money lost,
            // and a currency with no currency_types row refuses silently unless this is read.
            bool landed = await _grainFactory
                .GetPlayerWalletGrain(ctx.PlayerId)
                .GrantCurrencyAsync(
                    new CurrencyKind { CurrencyType = CurrencyType.Credits },
                    taken,
                    ct
                )
                .ConfigureAwait(true);

            if (!landed)
            {
                chest.Credits += taken;

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

                _logger.LogWarning(
                    "Wired chest {ChestId} in room {RoomId} could not pay {Amount} credits to player {PlayerId}; the chest keeps them.",
                    chestId,
                    RoomId,
                    taken,
                    ctx.PlayerId
                );

                return null;
            }

            return await SnapshotAsync(chestId, chest.Credits).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to withdraw from wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return null;
        }
    }

    /// <summary>Whether this player may operate that chest: it has to be a chest, standing in this
    /// room, and they have to be allowed to decorate.</summary>
    private async Task<bool> CanUseChestAsync(ActionContext ctx, int chestId)
    {
        if (
            ctx.PlayerId <= 0
            || !_state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || !IsChestClass(item.Definition.Name)
        )
        {
            return false;
        }

        RoomControllerType level = await SecurityModule
            .GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        return level != RoomControllerType.None;
    }

    private Task<WiredChestSnapshot?> SnapshotAsync(int chestId, int credits) =>
        Task.FromResult<WiredChestSnapshot?>(
            _state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
                ? new WiredChestSnapshot
                {
                    ChestId = chestId,
                    Credits = credits,
                    IsCoinChest = IsCoinChestClass(item.Definition.Name),
                }
                : null
        );

    public async Task<WiredChestSnapshot?> OpenWiredChestAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    )
    {
        if (
            ctx.PlayerId <= 0
            || !_state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || !IsChestClass(item.Definition.Name)
        )
        {
            return null;
        }

        // Only whoever may decorate the room may look inside: a chest is stock, and its contents are
        // not public just because the furni is.
        RoomControllerType level = await SecurityModule
            .GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        if (level == RoomControllerType.None)
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == chestId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (chest is null)
            {
                chest = new WiredChestEntity
                {
                    FurnitureEntityId = chestId,
                    Credits = 0,
                    NotificationsEnabled = true,
                };

                dbCtx.WiredChests.Add(chest);

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            }

            return new WiredChestSnapshot
            {
                ChestId = chestId,
                Credits = chest.Credits,
                IsCoinChest = IsCoinChestClass(item.Definition.Name),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to open wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return null;
        }
    }

    /// <summary>Turns a stored row into the item shape the chest screen and the inventory both
    /// speak. Owner name is left empty on purpose: the chest screen never shows it, and resolving a
    /// name per row would cost a lookup per item for nothing.</summary>
    private FurnitureItemSnapshot? ToChestItemSnapshot(FurnitureEntity entity)
    {
        FurnitureDefinitionSnapshot? definition = _definitionProvider.TryGetDefinition(
            entity.FurnitureDefinitionEntityId
        );

        if (definition is null)
        {
            return null;
        }

        return new FurnitureItemSnapshot
        {
            ItemId = entity.Id,
            SpriteId = definition.SpriteId,
            OwnerId = entity.PlayerEntityId,
            OwnerName = string.Empty,
            Definition = definition,
            StuffData = _stuffDataFactory
                .CreateStuffDataFromJson(definition.StuffDataType, entity.ExtraData)
                .GetSnapshot(),
            ExtraData = entity.ExtraData ?? string.Empty,
            SecondsToExpiration = -1,
            HasRentPeriodStarted = false,
            RoomId = -1,
        };
    }

    /// <summary>The kind the client names when it asks for "three of these": sprite and wall/floor,
    /// plus the poster number when the kind is a poster. It is the same expression the serializer
    /// uses to fill that field, so whatever we sent comes back matching.</summary>
    private static bool IsSameKind(
        FurnitureItemSnapshot item,
        bool isWallItem,
        int typeId,
        string legacyPosterId
    ) =>
        item.SpriteId == typeId
        && (item.Definition.ProductType == ProductType.Wall) == isWallItem
        && (
            item.Definition.FurniCategory != FurnitureCategory.Poster
            || item.ExtraData == legacyPosterId
        );

    public async Task<ImmutableArray<FurnitureItemSnapshot>?> ListWiredChestItemsAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    )
    {
        if (!await CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.AsNoTracking()
                .FirstOrDefaultAsync(c => c.FurnitureEntityId == chestId && c.DeletedAt == null, ct)
                .ConfigureAwait(true);

            // A chest nobody has opened yet has no row and holds nothing. That is an empty chest,
            // not a failure: the screen still has to open.
            if (chest is null)
            {
                return ImmutableArray<FurnitureItemSnapshot>.Empty;
            }

            List<FurnitureEntity> stored = await dbCtx
                .Furnitures.AsNoTracking()
                .Where(f => f.WiredChestEntityId == chest.Id && f.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            return [.. stored.Select(ToChestItemSnapshot).OfType<FurnitureItemSnapshot>()];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to list wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return null;
        }
    }

    public async Task<ImmutableArray<int>> WithdrawWiredChestItemsAsync(
        ActionContext ctx,
        int chestId,
        bool isWallItem,
        int typeId,
        string legacyPosterId,
        int count,
        CancellationToken ct
    )
    {
        if (count <= 0 || !await CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return [];
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.AsNoTracking()
                .FirstOrDefaultAsync(c => c.FurnitureEntityId == chestId && c.DeletedAt == null, ct)
                .ConfigureAwait(true);

            if (chest is null)
            {
                return [];
            }

            List<FurnitureEntity> stored = await dbCtx
                .Furnitures.Where(f => f.WiredChestEntityId == chest.Id && f.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            // The kind is decided on the snapshot rather than on the row: sprite, wall/floor and the
            // poster number all live on the definition or in the stuff data, not in columns.
            List<FurnitureEntity> leaving =
            [
                .. stored
                    .Where(entity =>
                    {
                        FurnitureItemSnapshot? snapshot = ToChestItemSnapshot(entity);

                        return snapshot is not null
                            && IsSameKind(snapshot, isWallItem, typeId, legacyPosterId);
                    })
                    .Take(count),
            ];

            if (leaving.Count == 0)
            {
                return [];
            }

            foreach (FurnitureEntity entity in leaving)
            {
                entity.WiredChestEntityId = null;
                entity.RoomEntityId = null;
                entity.PlayerEntityId = (int)ctx.PlayerId;
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            // The row is the player's now; the inventory grain still has to be told, because its
            // list is a cache built at activation and nothing reloads it on its own.
            await _grainFactory
                .GetInventoryGrain(ctx.PlayerId)
                .ReloadFurnitureAsync(ct)
                .ConfigureAwait(true);

            return [.. leaving.Select(entity => entity.Id)];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to withdraw items from wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return [];
        }
    }
}
