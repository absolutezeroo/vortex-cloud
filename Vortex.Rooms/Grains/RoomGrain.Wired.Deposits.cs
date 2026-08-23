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
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Rooms.Grains;

/// <summary>
/// Putting furniture into a wired chest.
/// </summary>
/// <remarks>
/// The deposit button opens a <em>trade</em>, not a dialog of its own: the client answers
/// <c>StartWiredChestDeposit</c> by waiting for the inventory's wired-trade screen, stakes furni
/// through it, and confirms in two steps. That is not a guess — <c>WiredTradingModel</c> is fully
/// ported and sends exactly those three messages, and nothing else on the client can move an item
/// into a chest.
/// <para>
/// Credits are deliberately not part of this. The player has no way to name an amount: the only
/// message the screen sends carries item ids, and the credits on each side are dictated by the
/// server in the table update. What the official server does for a coin chest cannot be read off
/// the client, so <c>DepositToWiredChestMessageHandler</c> keeps its stub for that half.
/// </para>
/// </remarks>
public sealed partial class RoomGrain
{
    /// <summary>How long a deposit may stay open before the client closes it on its own.</summary>
    private const int DepositTimeoutSeconds = 300;

    /// <summary>
    /// The requirement type that lets any tradeable furni be staked.
    /// </summary>
    /// <remarks>
    /// The client's <c>WiredTradeRequirementsModel.canOfferFurni()</c> branches on this: 0 takes
    /// only credit furniture, 1 refuses it, 2 takes anything, 4 reads a rules block. A chest takes
    /// what it is given, so 2 — and with no rules block to write, none is sent.
    /// </remarks>
    private const int DepositRequirementAnyFurni = 2;

    /// <summary>One player's open deposit.</summary>
    /// <remarks>
    /// Keyed by player rather than by chest: a player can only have one trade screen up, while a
    /// chest can be filled by several people at once.
    /// </remarks>
    private sealed record ChestDeposit(int ChestId, HashSet<int> ItemIds);

    private readonly Dictionary<PlayerId, ChestDeposit> _chestDeposits = [];

    /// <summary>Opens a deposit for a chest, if this player may fill it.</summary>
    /// <remarks>
    /// Filling is not the same permission as opening: a chest whose owner ticked "everyone can
    /// donate" takes from anyone, while looking inside still needs decorating rights.
    /// </remarks>
    public async Task<WiredDepositStart> StartWiredChestDepositAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || !_state.ItemsById.TryGetValue(chestId, out IRoomItem? item))
        {
            _logger.LogDebug(
                "Deposit refused: {ChestId} is not an item of room {RoomId}.",
                chestId,
                RoomId
            );

            return WiredDepositStart.Refused;
        }

        // Refusing tells the client nothing — it simply waits for a trade that never opens — so
        // each gate says which one it was. A refusal is ordinary, hence debug rather than warn.
        if (!IsChestLogic(item.Definition.LogicName))
        {
            _logger.LogDebug(
                "Deposit refused: {ChestId} has logic {Logic}, which is not a chest.",
                chestId,
                item.Definition.LogicName
            );

            return WiredDepositStart.Refused;
        }

        if (IsCoinChestLogic(item.Definition.LogicName))
        {
            _logger.LogDebug(
                "Deposit refused: {ChestId} is a coin chest, and the client cannot name an amount.",
                chestId
            );

            return WiredDepositStart.Refused;
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

            if (chest is not null && chest.Locked)
            {
                _logger.LogDebug("Deposit refused: chest {ChestId} is locked.", chestId);

                return WiredDepositStart.Refused;
            }

            if (chest is null || !chest.EveryoneCanDonate)
            {
                RoomControllerType level = await SecurityModule
                    .GetControllerLevelAsync(ctx)
                    .ConfigureAwait(true);

                if (level == RoomControllerType.None)
                {
                    _logger.LogDebug(
                        "Deposit refused: chest {ChestId} takes donations from rights-holders only, "
                            + "and player {PlayerId} has none here.",
                        chestId,
                        ctx.PlayerId
                    );

                    return WiredDepositStart.Refused;
                }
            }

            // Replacing tells the client to close the screen it already has up before opening this
            // one — true only when there really is one.
            bool replaced = _chestDeposits.ContainsKey(ctx.PlayerId);

            _chestDeposits[ctx.PlayerId] = new ChestDeposit(chestId, []);

            return replaced ? WiredDepositStart.Replaced : WiredDepositStart.Opened;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to open a deposit for wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return WiredDepositStart.Refused;
        }
    }

    /// <summary>Puts furniture on the table, or takes it off. Null when no deposit is open.</summary>
    /// <remarks>
    /// Every id is re-checked against the database rather than trusted: it has to be this player's,
    /// out of any room, not already inside a chest, and tradeable. A client that asks for someone
    /// else's furni gets it silently ignored, not an error — the table simply does not change.
    /// </remarks>
    public async Task<WiredDepositSnapshot?> UpdateWiredDepositItemsAsync(
        ActionContext ctx,
        bool remove,
        ImmutableArray<int> itemIds,
        CancellationToken ct
    )
    {
        if (!_chestDeposits.TryGetValue(ctx.PlayerId, out ChestDeposit? deposit))
        {
            return null;
        }

        if (remove)
        {
            foreach (int id in itemIds)
            {
                deposit.ItemIds.Remove(id);
            }
        }
        else
        {
            try
            {
                foreach (
                    int id in await ReadDepositableIdsAsync(ctx, itemIds, ct).ConfigureAwait(true)
                )
                {
                    deposit.ItemIds.Add(id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to stake items for a deposit in room {RoomId}.",
                    RoomId
                );

                return null;
            }
        }

        return await SnapshotDepositAsync(deposit, completed: false, ct).ConfigureAwait(true);
    }

    /// <summary>
    /// The accept button, and then the confirmation behind it.
    /// </summary>
    /// <remarks>
    /// The client sends this twice for one trade — false on the button, true from the dialog — and
    /// only the second may move anything. An empty table can be accepted and moves nothing, which
    /// is what the client's own accept gate already prevents.
    /// </remarks>
    public async Task<WiredDepositSnapshot?> AcceptWiredDepositAsync(
        ActionContext ctx,
        bool confirm,
        CancellationToken ct
    )
    {
        if (!_chestDeposits.TryGetValue(ctx.PlayerId, out ChestDeposit? deposit))
        {
            return null;
        }

        if (!confirm)
        {
            return await SnapshotDepositAsync(deposit, completed: false, ct).ConfigureAwait(true);
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == deposit.ChestId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            // A chest nobody has opened yet has no row, and a deposit is as good a first touch as
            // an open — the same reasoning OpenWiredChestAsync creates it under.
            if (chest is null)
            {
                chest = new WiredChestEntity
                {
                    FurnitureEntityId = deposit.ChestId,
                    Credits = 0,
                    NotificationsEnabled = true,
                };

                dbCtx.WiredChests.Add(chest);

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            }

            List<FurnitureEntity> moving = await dbCtx
                .Furnitures.Where(f =>
                    deposit.ItemIds.Contains(f.Id)
                    && f.PlayerEntityId == (int)ctx.PlayerId
                    && f.RoomEntityId == null
                    && f.WiredChestEntityId == null
                    && f.DeletedAt == null
                )
                .ToListAsync(ct)
                .ConfigureAwait(true);

            foreach (FurnitureEntity entity in moving)
            {
                entity.WiredChestEntityId = chest.Id;
            }

            if (moving.Count > 0)
            {
                dbCtx.WiredChestTransactions.Add(
                    NewManualTransaction(
                        ctx.PlayerId,
                        chest.Id,
                        depositFurni: moving.Count,
                        definitionInfo: string.Join(
                            ", ",
                            moving
                                .Select(entity =>
                                    _definitionProvider
                                        .TryGetDefinition(entity.FurnitureDefinitionEntityId)
                                        ?.Name
                                )
                                .Where(name => !string.IsNullOrEmpty(name))
                                .Distinct()
                        )
                    )
                );

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            }

            WiredDepositSnapshot snapshot = new()
            {
                ChestId = deposit.ChestId,
                Items = [.. moving.Select(ToChestItemSnapshot).OfType<FurnitureItemSnapshot>()],
                CanAccept = false,
                Completed = true,
            };

            _chestDeposits.Remove(ctx.PlayerId);

            // What the chest floats above itself is drawn from what it holds, which just changed.
            await ApplyChestSettingsToStuffDataAsync(deposit.ChestId, chest).ConfigureAwait(true);

            // And so do the windows other people have open on this chest — the handler answers only
            // the depositor.
            await NotifyOtherChestViewersAsync(
                    deposit.ChestId,
                    ctx.PlayerId,
                    new WiredChestItemsUpdateMessageComposer
                    {
                        ChestId = deposit.ChestId,
                        RemovedItemIds = ImmutableArray<int>.Empty,
                        AddedItems = snapshot.Items,
                    }
                )
                .ConfigureAwait(true);

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to complete a deposit into wired chest {ChestId} in room {RoomId}.",
                deposit.ChestId,
                RoomId
            );

            return null;
        }
    }

    /// <summary>Drops an open deposit. Nothing moved, so there is nothing to undo.</summary>
    public Task<bool> CancelWiredDepositAsync(ActionContext ctx, CancellationToken ct) =>
        Task.FromResult(_chestDeposits.Remove(ctx.PlayerId));

    /// <summary>The subset of the asked ids this player may actually put in a chest.</summary>
    private async Task<List<int>> ReadDepositableIdsAsync(
        ActionContext ctx,
        ImmutableArray<int> itemIds,
        CancellationToken ct
    )
    {
        if (itemIds.IsDefaultOrEmpty)
        {
            return [];
        }

        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        List<FurnitureEntity> rows = await dbCtx
            .Furnitures.AsNoTracking()
            .Where(f =>
                itemIds.Contains(f.Id)
                && f.PlayerEntityId == (int)ctx.PlayerId
                && f.RoomEntityId == null
                && f.WiredChestEntityId == null
                && f.DeletedAt == null
            )
            .ToListAsync(ct)
            .ConfigureAwait(true);

        return
        [
            .. rows.Where(entity =>
                    _definitionProvider
                        .TryGetDefinition(entity.FurnitureDefinitionEntityId)
                        ?.CanTrade == true
                )
                .Select(entity => entity.Id),
        ];
    }

    /// <summary>Reads the staked rows back out so the table can be redrawn from the database.</summary>
    private async Task<WiredDepositSnapshot?> SnapshotDepositAsync(
        ChestDeposit deposit,
        bool completed,
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<FurnitureEntity> rows = await dbCtx
                .Furnitures.AsNoTracking()
                .Where(f => deposit.ItemIds.Contains(f.Id) && f.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            return new WiredDepositSnapshot
            {
                ChestId = deposit.ChestId,
                Items = [.. rows.Select(ToChestItemSnapshot).OfType<FurnitureItemSnapshot>()],
                CanAccept = deposit.ItemIds.Count > 0,
                Completed = completed,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read a deposit's table in room {RoomId}.", RoomId);

            return null;
        }
    }
}
