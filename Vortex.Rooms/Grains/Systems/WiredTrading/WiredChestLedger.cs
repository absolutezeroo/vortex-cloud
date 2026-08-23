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
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
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
using Vortex.Rooms.Grains;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Grains.Systems.WiredTrading;

/// <summary>
/// The history of what moved: the one reader and the one writer of <c>wired_chest_transactions</c>.
/// </summary>
/// <remarks>
/// A ledger entry is not the live trade and not the client's "transaction" either -- it is the
/// record left behind once something has actually moved. Three builders used to spell that out in
/// two files, and each was free to drift from the rule they all state: a movement that was rolled
/// back never happened, and a log that says otherwise is worse than no log.
/// </remarks>
public sealed class WiredChestLedger(RoomGrain roomGrain, WiredChestStore store)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    private readonly WiredChestStore _store = store;

    /// <summary>What the client calls MANUAL.</summary>
    private const int ManualTransaction = 0;

    /// <summary>The client's own transaction types, as the log screen localises them.</summary>
    private const int ContractPaymentTransaction = 2;

    private const int ContractRewardTransaction = 3;

    private const int ContractTradeTransaction = 4;

    /// <summary>What the client calls WIRED: a box moved this, not a person.</summary>
    private const int WiredTransaction = 1;

    /// <summary>The two log lists the client knows: a chest, or the room the chests stand in. It
    /// reads the type back off the page to decide what it is looking at.</summary>
    private const int ChestLogList = 0;
    private const int RoomLogList = 1;

    /// <summary>The page size comes straight from the client, so it is clamped. Asking for a
    /// million rows should cost a page, not the table.</summary>
    private const int MaxLogPageSize = 200;

    /// <summary>A row for something a person did by hand. Type 0 is the client's MANUAL; the wired,
    /// contract and auto-withdraw types need machinery this hotel does not have yet.</summary>
    internal WiredChestTransactionEntity NewManualTransaction(
        PlayerId playerId,
        int chestRowId,
        int withdrawCoins = 0,
        int withdrawFurni = 0,
        int depositFurni = 0,
        string definitionInfo = ""
    ) =>
        new()
        {
            WiredChestEntityId = chestRowId,
            RoomEntityId = (int)_roomGrain.RoomId,
            TransactionType = ManualTransaction,
            DefinitionInfo = definitionInfo,
            PlayerEntityId = (int)playerId,
            PlayerName = ResolvePlayerName(playerId),
            // One row, one chest — the entity says as much, and nothing a player does by hand
            // touches two. The zero below is a fact, not a placeholder: nothing a player does by
            // hand puts coins into a chest, because the client has no way to name an amount. A
            // contract does, and writes its own row — the amount there is the contract's, not
            // something anyone typed.
            ChestCount = 1,
            WithdrawFurniCount = withdrawFurni,
            DepositFurniCount = depositFurni,
            WithdrawCoinsCount = withdrawCoins,
            DepositCoinsCount = 0,
        };

    internal WiredChestTransactionEntity NewWiredTransaction(
        PlayerId playerId,
        int chestRowId,
        int withdrawCoins = 0,
        int withdrawFurni = 0,
        string definitionInfo = ""
    ) =>
        new()
        {
            WiredChestEntityId = chestRowId,
            RoomEntityId = (int)_roomGrain.RoomId,
            TransactionType = WiredTransaction,
            DefinitionInfo = definitionInfo,
            PlayerEntityId = (int)playerId,
            PlayerName = ResolvePlayerName(playerId),
            // Same one-row-one-chest reading as the manual builder. Both deposit counters are zero
            // by what this path is: wiring pays *out* of a chest, it never fills one.
            ChestCount = 1,
            WithdrawFurniCount = withdrawFurni,
            DepositFurniCount = 0,
            WithdrawCoinsCount = withdrawCoins,
            DepositCoinsCount = 0,
        };

    /// <summary>
    /// A row for a contract, which is the one kind that fills all four counters.
    /// </summary>
    /// <remarks>
    /// The type says which way it went — payment, reward, or both — because that is what the log
    /// screen localises, and a trade that only takes reads wrong under the name of one that gives.
    /// This is also the only path that deposits coins into a chest: the amount is the contract's,
    /// not something the player typed, which is why the manual builder still writes a zero there.
    /// </remarks>
    internal WiredChestTransactionEntity NewContractTransaction(
        PlayerId playerId,
        int chestRowId,
        ContractCharge payment,
        List<FurnitureEntity> paying,
        ContractCharge reward,
        List<FurnitureEntity> giving
    )
    {
        bool paid = !payment.IsNothing;
        bool given = !reward.IsNothing;

        return new WiredChestTransactionEntity
        {
            WiredChestEntityId = chestRowId,
            RoomEntityId = (int)_roomGrain.RoomId,
            TransactionType = (paid, given) switch
            {
                (true, true) => ContractTradeTransaction,
                (false, true) => ContractRewardTransaction,
                _ => ContractPaymentTransaction,
            },
            DefinitionInfo = DescribeItems(paying.Concat(giving)),
            PlayerEntityId = (int)playerId,
            PlayerName = ResolvePlayerName(playerId),
            ChestCount = 1,
            WithdrawFurniCount = giving.Count,
            DepositFurniCount = paying.Count,
            WithdrawCoinsCount = reward.Coins,
            DepositCoinsCount = payment.Coins,
        };
    }

    /// <summary>The names of what moved, in the order they moved, once per item.</summary>
    /// <remarks>
    /// Repeated rather than distinct, because the details screen counts them back out of this
    /// string — collapsing duplicates here turns "3x sofa" into "sofa" there.
    /// </remarks>
    private string DescribeItems(IEnumerable<FurnitureEntity> items) =>
        string.Join(
            ", ",
            items
                .Select(entity =>
                    _roomGrain
                        ._definitionProvider.TryGetDefinition(entity.FurnitureDefinitionEntityId)
                        ?.Name
                )
                .Where(name => !string.IsNullOrEmpty(name))
        );

    /// <summary>The player's name as it is right now, for a row that will outlive it. Taken from the
    /// room rather than the database: whoever moves something is standing in it.</summary>
    private string ResolvePlayerName(PlayerId playerId) =>
        _roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
        && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
            ? avatar.Name
            : string.Empty;

    private static WiredTransactionSnapshot ToTransactionSnapshot(
        WiredChestTransactionEntity row
    ) =>
        new()
        {
            TransactionId = row.Id,
            RoomId = row.RoomEntityId,
            TransactionType = row.TransactionType,
            DefinitionInfo = row.DefinitionInfo,
            PlayerId = row.PlayerEntityId,
            PlayerName = row.PlayerName,
            Timestamp = new DateTimeOffset(row.CreatedAt, TimeSpan.Zero).ToUnixTimeMilliseconds(),
            ReadableTimestamp = row.CreatedAt.ToString(
                "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture
            ),
            ChestCount = row.ChestCount,
            WithdrawFurniCount = row.WithdrawFurniCount,
            DepositFurniCount = row.DepositFurniCount,
            WithdrawCoinsCount = row.WithdrawCoinsCount,
            DepositCoinsCount = row.DepositCoinsCount,
        };

    private async Task<WiredTransactionsSnapshot?> ReadTransactionsAsync(
        int logListType,
        long logListId,
        int? chestRowId,
        int pageSize,
        int page,
        CancellationToken ct
    )
    {
        int size = Math.Clamp(pageSize, 1, MaxLogPageSize);
        int wanted = Math.Max(1, page);

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            IQueryable<WiredChestTransactionEntity> rows = dbCtx
                .WiredChestTransactions.AsNoTracking()
                .Where(t => t.DeletedAt == null);

            rows = chestRowId is null
                ? rows.Where(t => t.RoomEntityId == (int)_roomGrain.RoomId)
                : rows.Where(t => t.WiredChestEntityId == chestRowId);

            int total = await rows.CountAsync(ct).ConfigureAwait(true);

            List<WiredChestTransactionEntity> paged = await rows.OrderByDescending(t => t.Id)
                .Skip((wanted - 1) * size)
                .Take(size)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            return new WiredTransactionsSnapshot
            {
                LogListType = logListType,
                LogListId = logListId,
                TotalLogs = total,
                CurrentPage = wanted,
                Amount = size,
                Logs = [.. paged.Select(ToTransactionSnapshot)],
            };
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to read chest transactions in room {RoomId}.",
                _roomGrain.RoomId
            );

            return null;
        }
    }

    public async Task<WiredTransactionsSnapshot?> GetWiredChestTransactionsAsync(
        ActionContext ctx,
        int chestId,
        int pageSize,
        int page,
        CancellationToken ct
    )
    {
        if (!await _store.CanUseChestAsync(ctx, chestId).ConfigureAwait(true))
        {
            return null;
        }

        int? chestRowId = await ResolveChestRowIdAsync(chestId, ct).ConfigureAwait(true);

        // A chest nobody has touched has no row and therefore no history. That is an empty page,
        // not a refusal: the screen still opens.
        return await ReadTransactionsAsync(
                ChestLogList,
                chestId,
                chestRowId ?? -1,
                pageSize,
                page,
                ct
            )
            .ConfigureAwait(true);
    }

    /// <summary>
    /// One row of the log, opened.
    /// </summary>
    /// <remarks>
    /// The breakdown is rebuilt from the row's own <c>DefinitionInfo</c> — the names of what moved,
    /// in order, repeated once per item — because that is what a row stores. Names resolve back to
    /// definitions, and two of the same name are one line with a count, which is exactly what the
    /// window renders. A name that no longer resolves is dropped and makes the answer incomplete,
    /// so the window says "and more" rather than quietly under-reporting.
    /// <para>
    /// Withdrawals and deposits are told apart by the row's own counters, not by the text: one row
    /// is one direction, and a row that moved nothing has an empty breakdown either way.
    /// </para>
    /// </remarks>
    public async Task<WiredTransactionDetailsSnapshot?> GetWiredTransactionDetailsAsync(
        ActionContext ctx,
        long transactionId,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || transactionId <= 0)
        {
            return null;
        }

        RoomControllerType level = await _roomGrain
            .SecurityModule.GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        if (level == RoomControllerType.None)
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestTransactionEntity? row = await dbCtx
                .WiredChestTransactions.AsNoTracking()
                .FirstOrDefaultAsync(
                    t =>
                        t.Id == transactionId
                        && t.RoomEntityId == (int)_roomGrain.RoomId
                        && t.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (row is null)
            {
                return null;
            }

            int chestFurniId = await dbCtx
                .WiredChests.AsNoTracking()
                .Where(chest => chest.Id == row.WiredChestEntityId)
                .Select(chest => chest.FurnitureEntityId)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(true);

            List<WiredTransactionItemCount> moved = BuildItemBreakdown(
                row.DefinitionInfo,
                out bool incomplete
            );

            bool isWithdrawal = row.WithdrawFurniCount > 0;

            return new WiredTransactionDetailsSnapshot
            {
                Info = ToTransactionSnapshot(row),
                ChestIds = chestFurniId > 0 ? [chestFurniId] : [],
                Deposited = isWithdrawal ? [] : [.. moved],
                Withdrawn = isWithdrawal ? [.. moved] : [],
                IsIncompleteData = incomplete,
            };
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to read transaction {TransactionId} in room {RoomId}.",
                transactionId,
                _roomGrain.RoomId
            );

            return null;
        }
    }

    /// <summary>
    /// The names a row recorded, counted by kind.
    /// </summary>
    /// <remarks>
    /// A row stores what moved as names joined by ", ", so a name containing that separator would
    /// split wrongly — none does, and furniture names are database identifiers rather than free
    /// text. <paramref name="incomplete" /> is set when a name resolves to nothing, which is the
    /// only way this can lose an item.
    /// </remarks>
    private List<WiredTransactionItemCount> BuildItemBreakdown(
        string definitionInfo,
        out bool incomplete
    )
    {
        incomplete = false;

        List<WiredTransactionItemCount> items = [];

        if (string.IsNullOrWhiteSpace(definitionInfo))
        {
            return items;
        }

        Dictionary<int, WiredTransactionItemCount> byDefinition = [];

        foreach (string name in definitionInfo.Split(',', StringSplitOptions.TrimEntries))
        {
            FurnitureDefinitionSnapshot? definition =
                _roomGrain._definitionProvider.TryGetDefinitionByName(name);

            if (definition is null)
            {
                incomplete = true;

                continue;
            }

            byDefinition[definition.Id] = byDefinition.TryGetValue(
                definition.Id,
                out WiredTransactionItemCount? seen
            )
                ? seen with
                {
                    Count = seen.Count + 1,
                }
                : new WiredTransactionItemCount
                {
                    IsWallItem = definition.ProductType == ProductType.Wall,
                    SpriteId = definition.SpriteId,
                    LegacyPosterId = string.Empty,
                    Count = 1,
                };
        }

        items.AddRange(byDefinition.Values);

        return items;
    }

    public async Task<WiredTransactionsSnapshot?> GetWiredRoomTransactionsAsync(
        ActionContext ctx,
        int pageSize,
        int page,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return null;
        }

        RoomControllerType level = await _roomGrain
            .SecurityModule.GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        if (level == RoomControllerType.None)
        {
            return null;
        }

        return await ReadTransactionsAsync(
                RoomLogList,
                (int)_roomGrain.RoomId,
                null,
                pageSize,
                page,
                ct
            )
            .ConfigureAwait(true);
    }

    private async Task<int?> ResolveChestRowIdAsync(int chestId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        WiredChestEntity? chest = await WiredChestStore
            .ReadAsync(dbCtx, chestId, ct)
            .ConfigureAwait(true);

        return chest?.Id;
    }
}
