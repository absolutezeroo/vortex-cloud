using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
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
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Rooms.Grains;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Grains.Systems.WiredTrading;

/// <summary>
/// Taking value back out of a chest: by hand from the screen, or by a wired box paying it out.
/// </summary>
/// <remarks>
/// The chest is emptied first and refilled if the wallet refuses. Money that left a chest and
/// reached nobody is money lost, and a currency with no <c>currency_types</c> row refuses silently
/// unless the answer is read.
/// </remarks>
public sealed partial class WiredTradeSettlement
{
    public async Task<WiredChestSnapshot?> WithdrawWiredChestCreditsAsync(
        ActionContext ctx,
        int chestId,
        int amount,
        CancellationToken ct
    )
    {
        if (!await _store.CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await WiredChestStore
                .FindAsync(dbCtx, chestId, ct)
                .ConfigureAwait(true);

            // A locked chest refuses withdrawals — the wired pay-out already reads it this way, and
            // a lock the owner's own withdraw button walks straight through is not a lock.
            if (chest is null || chest.Credits <= 0 || chest.Locked)
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
            bool landed = await _roomGrain
                ._grainFactory.GetPlayerWalletGrain(ctx.PlayerId)
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

                _roomGrain._logger.LogWarning(
                    "Wired chest {ChestId} in room {RoomId} could not pay {Amount} credits to player {PlayerId}; the chest keeps them.",
                    chestId,
                    _roomGrain.RoomId,
                    taken,
                    ctx.PlayerId
                );

                return null;
            }

            // Logged only here, past the wallet: a movement that was rolled back never happened,
            // and a log that says otherwise is worse than no log.
            dbCtx.WiredChestTransactions.Add(
                _ledger.NewManualTransaction(ctx.PlayerId, chest.Id, withdrawCoins: taken)
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            return await _store.SnapshotAsync(chestId, chest.Credits).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to withdraw from wired chest {ChestId} in room {RoomId}.",
                chestId,
                _roomGrain.RoomId
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
        if (count <= 0 || !await _store.CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return [];
        }

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await WiredChestStore
                .ReadAsync(dbCtx, chestId, ct)
                .ConfigureAwait(true);

            // A locked chest refuses withdrawals, furniture included: the lock guards what leaves,
            // and taking the stock out by hand is the plainest way for it to leave.
            if (chest is null || chest.Locked)
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
                        FurnitureItemSnapshot? snapshot = _store.ToChestItemSnapshot(entity);

                        return snapshot is not null
                            && WiredChestStore.IsSameKind(
                                snapshot,
                                isWallItem,
                                typeId,
                                legacyPosterId
                            );
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

            dbCtx.WiredChestTransactions.Add(
                _ledger.NewManualTransaction(
                    ctx.PlayerId,
                    chest.Id,
                    withdrawFurni: leaving.Count,
                    definitionInfo: string.Join(
                        ", ",
                        leaving
                            .Select(entity =>
                                _roomGrain
                                    ._definitionProvider.TryGetDefinition(
                                        entity.FurnitureDefinitionEntityId
                                    )
                                    ?.Name
                                ?? string.Empty
                            )
                            .Distinct()
                    )
                )
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            // The row is the player's now; the inventory grain still has to be told, because its
            // list is a cache built at activation and nothing reloads it on its own.
            await _roomGrain
                ._grainFactory.GetInventoryGrain(ctx.PlayerId)
                .ReloadFurnitureAsync(ct)
                .ConfigureAwait(true);

            // The preview is drawn from what the chest holds, so it is now stale.
            await _store.ApplyChestSettingsToStuffDataAsync(chestId, chest).ConfigureAwait(true);

            ImmutableArray<int> removed = [.. leaving.Select(entity => entity.Id)];

            // The caller answers the player who asked; these are the other windows open on the
            // same chest, which would otherwise keep showing rows that have left.
            await _system
                .NotifyOtherChestViewersAsync(
                    chestId,
                    ctx.PlayerId,
                    new WiredChestItemsUpdateMessageComposer
                    {
                        ChestId = chestId,
                        RemovedItemIds = removed,
                        AddedItems = ImmutableArray<FurnitureItemSnapshot>.Empty,
                    }
                )
                .ConfigureAwait(true);

            return removed;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to withdraw items from wired chest {ChestId} in room {RoomId}.",
                chestId,
                _roomGrain.RoomId
            );

            return [];
        }
    }

    public async Task<int> PayOutWiredChestCreditsAsync(
        int chestId,
        PlayerId playerId,
        int amount,
        bool everything,
        CancellationToken ct
    )
    {
        if (
            playerId <= 0
            || (!everything && amount <= 0)
            || !_roomGrain._state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || !WiredChestStore.IsCoinChestLogic(item.Definition.LogicName)
        )
        {
            return 0;
        }

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await WiredChestStore
                .FindAsync(dbCtx, chestId, ct)
                .ConfigureAwait(true);

            if (chest is null || chest.Credits <= 0 || chest.Locked)
            {
                return 0;
            }

            int taken = everything ? chest.Credits : Math.Min(amount, chest.Credits);

            chest.Credits -= taken;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            bool landed = await _roomGrain
                ._grainFactory.GetPlayerWalletGrain(playerId)
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

                return 0;
            }

            dbCtx.WiredChestTransactions.Add(
                _ledger.NewWiredTransaction(playerId, chest.Id, withdrawCoins: taken)
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            await _store.ApplyChestSettingsToStuffDataAsync(chestId, chest).ConfigureAwait(true);

            return taken;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Wired pay-out failed for chest {ChestId} in room {RoomId}.",
                chestId,
                _roomGrain.RoomId
            );

            return 0;
        }
    }

    public async Task<int> PayOutWiredChestItemsAsync(
        int chestId,
        PlayerId playerId,
        int count,
        CancellationToken ct
    )
    {
        if (
            playerId <= 0
            || count <= 0
            || !_roomGrain._state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || !WiredChestStore.IsChestLogic(item.Definition.LogicName)
            || WiredChestStore.IsCoinChestLogic(item.Definition.LogicName)
        )
        {
            return 0;
        }

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await WiredChestStore
                .FindAsync(dbCtx, chestId, ct)
                .ConfigureAwait(true);

            if (chest is null || chest.Locked)
            {
                return 0;
            }

            List<FurnitureEntity> leaving = await dbCtx
                .Furnitures.Where(f => f.WiredChestEntityId == chest.Id && f.DeletedAt == null)
                .Take(count)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            if (leaving.Count == 0)
            {
                return 0;
            }

            foreach (FurnitureEntity entity in leaving)
            {
                entity.WiredChestEntityId = null;
                entity.RoomEntityId = null;
                entity.PlayerEntityId = (int)playerId;
            }

            dbCtx.WiredChestTransactions.Add(
                _ledger.NewWiredTransaction(playerId, chest.Id, withdrawFurni: leaving.Count)
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            await _roomGrain
                ._grainFactory.GetInventoryGrain(playerId)
                .ReloadFurnitureAsync(ct)
                .ConfigureAwait(true);

            return leaving.Count;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Wired furni pay-out failed for chest {ChestId} in room {RoomId}.",
                chestId,
                _roomGrain.RoomId
            );

            return 0;
        }
    }
}
