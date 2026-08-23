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
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Grains;

/// <summary>
/// Settling a contract: taking what it asks for and handing back what it promises.
/// </summary>
/// <remarks>
/// A contract trade is a deposit with a price on it, and the client says so — the same three
/// messages drive both, because the offer arrives on the same trading screen. So it runs on the
/// same session, and everything here is about the half a plain deposit does not have: is the stake
/// enough, can the chest pay, and moving both sides at once.
/// <para>
/// The stock is the chest the offering box points at (its first furni picker, "chests"; the second
/// is the contract itself). Payment goes in there and the reward comes out of there — a contract is
/// a shop counter, and the chest behind it is the shop.
/// </para>
/// <para>
/// Nothing moves until everything is known to be movable. The order is: read the stake, find a rule
/// it satisfies, check the chest can pay, debit the player, and only then write. The one step that
/// can still fail after the write is crediting the reward, and that is compensated the way the
/// chest pay-out already compensates it.
/// </para>
/// </remarks>
public sealed partial class RoomGrain
{
    /// <summary>The client's own transaction types, as the log screen localises them.</summary>
    private const int ContractPaymentTransaction = 2;

    private const int ContractRewardTransaction = 3;

    private const int ContractTradeTransaction = 4;

    /// <summary>
    /// Puts a contract's trade screen up for a player, with the chest that backs it.
    /// </summary>
    /// <remarks>
    /// One screen per player, so this replaces whatever was open — including a plain deposit, which
    /// is the same reading the offer itself takes when it withdraws a previous offer.
    /// </remarks>
    private void OpenContractSession(
        PlayerId playerId,
        int contractId,
        int chestId,
        TradeContract contract,
        int multiplier
    ) =>
        _chestDeposits[playerId] = new ChestDeposit(chestId, [])
        {
            Contract = contract,
            ContractId = contractId,
            Multiplier = Math.Max(1, multiplier),
        };

    /// <summary>
    /// The rows a contract can see, in the terms it is written in.
    /// </summary>
    /// <remarks>
    /// A row that cannot be turned into a chest item is one this hotel cannot describe, and an item
    /// nobody can describe is one no term can name — so it drops out rather than matching
    /// everything.
    /// </remarks>
    private List<ContractItem> AsContractItems(IEnumerable<FurnitureEntity> rows) =>
        [
            .. rows.Select(entity => (entity, snapshot: ToChestItemSnapshot(entity)))
                .Where(pair => pair.snapshot is not null)
                .Select(pair => new ContractItem(
                    pair.entity.Id,
                    pair.snapshot!.Definition.ProductType == ProductType.Wall,
                    pair.snapshot.SpriteId,
                    pair.snapshot.ExtraData ?? string.Empty,
                    pair.snapshot.Definition.FurniCategory == FurnitureCategory.Poster
                )),
        ];

    /// <summary>
    /// The accept button on a contract trade.
    /// </summary>
    /// <remarks>
    /// Answers with the table left open when the price is not met — the player can add to the stake
    /// and accept again — and with a completed one only once both sides have actually moved. It
    /// never answers "done" for a trade that did nothing.
    /// </remarks>
    private async Task<WiredDepositSnapshot?> SettleContractAsync(
        ActionContext ctx,
        ChestDeposit session,
        CancellationToken ct
    )
    {
        // A settlement banks the stake in a chest and pays the reward out of one, so the box has to
        // have been pointed at a chest. Without that, the row this would otherwise create is keyed
        // to whatever furni came first — a contract, a lamp, or id 0 — and what is paid into it is
        // reachable by nobody.
        if (
            !_state.ItemsById.TryGetValue(session.ChestId, out IRoomItem? chestItem)
            || !IsChestLogic(chestItem.Definition.LogicName)
        )
        {
            _logger.LogWarning(
                "Contract settlement refused in room {RoomId}: {ChestId} is not a chest.",
                RoomId,
                session.ChestId
            );

            return await SnapshotContractAsync(session, staked: null, completed: false, ct)
                .ConfigureAwait(true);
        }

        // Taken from the wallet before anything moves, so it has to go back if the move fails.
        int refundable = 0;

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == session.ChestId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (chest is null)
            {
                chest = new WiredChestEntity
                {
                    FurnitureEntityId = session.ChestId,
                    Credits = 0,
                    NotificationsEnabled = true,
                };

                dbCtx.WiredChests.Add(chest);

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            }

            List<FurnitureEntity> staked = await dbCtx
                .Furnitures.Where(f =>
                    session.ItemIds.Contains(f.Id)
                    && f.PlayerEntityId == (int)ctx.PlayerId
                    && f.RoomEntityId == null
                    && f.WiredChestEntityId == null
                    && f.DeletedAt == null
                )
                .ToListAsync(ct)
                .ConfigureAwait(true);

            List<FurnitureEntity> stock = await dbCtx
                .Furnitures.Where(f => f.WiredChestEntityId == chest.Id && f.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            ContractCharge? payment = WiredContractSettlement.MatchStake(
                session.Contract!,
                session.Multiplier,
                AsContractItems(staked)
            );

            if (
                payment is null
                || !WiredContractSettlement.TryReserveReward(
                    session.Contract!,
                    session.Multiplier,
                    AsContractItems(stock),
                    chest.Credits,
                    out ContractCharge reward
                )
            )
            {
                // Not a failure of the trade, only of this attempt: the screen stays up.
                return await SnapshotContractAsync(session, staked, completed: false, ct)
                    .ConfigureAwait(true);
            }

            // A locked chest lets nothing out — see WiredChestEntity.Locked. Taking payment into
            // one is still fine: what the lock guards is the leaving half.
            if (chest.Locked && !reward.IsNothing)
            {
                return await SnapshotContractAsync(session, staked, completed: false, ct)
                    .ConfigureAwait(true);
            }

            if (
                payment.Coins > 0
                && !await TryTakeCreditsAsync(ctx.PlayerId, payment.Coins, ct).ConfigureAwait(true)
            )
            {
                return await SnapshotContractAsync(session, staked, completed: false, ct)
                    .ConfigureAwait(true);
            }

            refundable = payment.Coins;

            List<FurnitureEntity> paying =
            [
                .. staked.Where(row => payment.ItemIds.Contains(row.Id)),
            ];
            List<FurnitureEntity> giving = [.. stock.Where(row => reward.ItemIds.Contains(row.Id))];

            foreach (FurnitureEntity entity in paying)
            {
                entity.WiredChestEntityId = chest.Id;
            }

            foreach (FurnitureEntity entity in giving)
            {
                entity.WiredChestEntityId = null;
                entity.RoomEntityId = null;
                entity.PlayerEntityId = (int)ctx.PlayerId;
            }

            chest.Credits += payment.Coins - reward.Coins;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            // The goods have moved; the payment is part of a trade that happened and is no longer
            // owed back.
            refundable = 0;

            // The one step that can still fail with the goods already moved, so it is the one step
            // that undoes itself — the chest keeps what it could not hand over.
            int paidOut = reward.Coins;

            if (
                reward.Coins > 0
                && !await GiveCreditsAsync(ctx.PlayerId, reward.Coins, ct).ConfigureAwait(true)
            )
            {
                chest.Credits += reward.Coins;

                paidOut = 0;
            }

            // The books last, and written from what actually landed rather than from what was
            // meant to: a movement that was rolled back never happened, and a log that says
            // otherwise is worse than no log.
            dbCtx.WiredChestTransactions.Add(
                NewContractTransaction(
                    ctx.PlayerId,
                    chest.Id,
                    payment,
                    paying,
                    reward with
                    {
                        Coins = paidOut,
                    },
                    giving
                )
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            _chestDeposits.Remove(ctx.PlayerId);

            await CompleteTransactionAsync(ctx.PlayerId, ct).ConfigureAwait(true);

            await AnnounceContractSuccessAsync(ctx.PlayerId, session, ct).ConfigureAwait(true);

            if (giving.Count > 0)
            {
                // The row is the player's now; the inventory grain's list is a cache built at
                // activation and nothing reloads it on its own.
                await _grainFactory
                    .GetInventoryGrain(ctx.PlayerId)
                    .ReloadFurnitureAsync(ct)
                    .ConfigureAwait(true);
            }

            await ApplyChestSettingsToStuffDataAsync(session.ChestId, chest).ConfigureAwait(true);

            await NotifyOtherChestViewersAsync(
                    session.ChestId,
                    ctx.PlayerId,
                    new WiredChestItemsUpdateMessageComposer
                    {
                        ChestId = session.ChestId,
                        RemovedItemIds = reward.ItemIds,
                        AddedItems =
                        [
                            .. paying.Select(ToChestItemSnapshot).OfType<FurnitureItemSnapshot>(),
                        ],
                    }
                )
                .ConfigureAwait(true);

            return new WiredDepositSnapshot
            {
                ChestId = session.ChestId,
                Items = [.. paying.Select(ToChestItemSnapshot).OfType<FurnitureItemSnapshot>()],
                CanAccept = false,
                Completed = true,
                RewardItems =
                [
                    .. giving.Select(ToChestItemSnapshot).OfType<FurnitureItemSnapshot>(),
                ],
                RewardCredits = paidOut,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to settle a contract against chest {ChestId} in room {RoomId}.",
                session.ChestId,
                RoomId
            );

            // The payment left the wallet before the goods could move, so it goes back: coins taken
            // for a trade that did not happen are coins stolen, and the screen is still open for a
            // second attempt that would charge again. Compensated on no token, because a cancelled
            // request is exactly the case that must not skip this.
            if (
                refundable > 0
                && !await GiveCreditsAsync(ctx.PlayerId, refundable, CancellationToken.None)
                    .ConfigureAwait(true)
            )
            {
                _logger.LogError(
                    "Player {PlayerId} paid {Coins} credits for a contract that failed to settle "
                        + "in room {RoomId}, and the refund was refused.",
                    ctx.PlayerId,
                    refundable,
                    RoomId
                );
            }

            return null;
        }
    }

    /// <summary>
    /// Tells the player their contract went through, and what it gave them.
    /// </summary>
    /// <remarks>
    /// A reward contract is the only one that carries anything beyond the announcement — its own
    /// editor is where the pop-up text and the open-by-default flag are written, so they are read
    /// back from the contract rather than invented here. A contract that promises nothing still
    /// announces itself: the notification is what tells the player the trade closed.
    /// </remarks>
    private async Task AnnounceContractSuccessAsync(
        PlayerId playerId,
        ChestDeposit session,
        CancellationToken ct
    )
    {
        WiredContractSnapshot? written = await ReadStoredContractAsync(session.ContractId, ct)
            .ConfigureAwait(true);

        await _grainFactory
            .GetPlayerPresenceGrain(playerId)
            .SendComposerAsync(
                new WiredTransactionSuccessMessageComposer
                {
                    TransactionSuccessTypeId = written?.ContractType ?? 0,
                    Reward = written?.YouGetRule,
                    RewardText = written?.RewardText ?? string.Empty,
                    OpenByDefault = written?.ShowDialog ?? false,
                }
            )
            .ConfigureAwait(true);
    }

    /// <summary>The table as a contract trade draws it: the stake on one side, the reward on the other.</summary>
    /// <remarks>
    /// The reward shown is what the chest could pay right now, so a shop that has just run out says
    /// so by showing less — and the accept button goes dead with it rather than accepting into a
    /// refusal.
    /// </remarks>
    private async Task<WiredDepositSnapshot?> SnapshotContractAsync(
        ChestDeposit session,
        List<FurnitureEntity>? staked,
        bool completed,
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == session.ChestId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            staked ??= await dbCtx
                .Furnitures.AsNoTracking()
                .Where(f => session.ItemIds.Contains(f.Id) && f.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            List<FurnitureEntity> stock = chest is null
                ? []
                : await dbCtx
                    .Furnitures.AsNoTracking()
                    .Where(f => f.WiredChestEntityId == chest.Id && f.DeletedAt == null)
                    .ToListAsync(ct)
                    .ConfigureAwait(true);

            bool priceMet =
                WiredContractSettlement.MatchStake(
                    session.Contract!,
                    session.Multiplier,
                    AsContractItems(staked)
                )
                is not null;

            bool chestCanPay = WiredContractSettlement.TryReserveReward(
                session.Contract!,
                session.Multiplier,
                AsContractItems(stock),
                chest?.Credits ?? 0,
                out ContractCharge reward
            );

            return new WiredDepositSnapshot
            {
                ChestId = session.ChestId,
                Items = [.. staked.Select(ToChestItemSnapshot).OfType<FurnitureItemSnapshot>()],
                CanAccept = priceMet && chestCanPay,
                Completed = completed,
                RewardItems =
                [
                    .. stock
                        .Where(row => reward.ItemIds.Contains(row.Id))
                        .Select(ToChestItemSnapshot)
                        .OfType<FurnitureItemSnapshot>(),
                ],
                RewardCredits = reward.Coins,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to draw a contract table against chest {ChestId} in room {RoomId}.",
                session.ChestId,
                RoomId
            );

            return null;
        }
    }

    /// <summary>Takes the coins a contract asks for. False leaves the player's wallet untouched.</summary>
    private async Task<bool> TryTakeCreditsAsync(
        PlayerId playerId,
        int amount,
        CancellationToken ct
    )
    {
        WalletDebitResult result = await _grainFactory
            .GetPlayerWalletGrain(playerId)
            .TryDebitAsync(
                [
                    new WalletDebitRequest
                    {
                        CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                        Amount = amount,
                    },
                ],
                ct
            )
            .ConfigureAwait(true);

        return result.Succeeded;
    }

    private Task<bool> GiveCreditsAsync(PlayerId playerId, int amount, CancellationToken ct) =>
        _grainFactory
            .GetPlayerWalletGrain(playerId)
            .GrantCurrencyAsync(
                new CurrencyKind { CurrencyType = CurrencyType.Credits },
                amount,
                ct
            );

    /// <summary>
    /// A row for a contract, which is the one kind that fills all four counters.
    /// </summary>
    /// <remarks>
    /// The type says which way it went — payment, reward, or both — because that is what the log
    /// screen localises, and a trade that only takes reads wrong under the name of one that gives.
    /// This is also the only path that deposits coins into a chest: the amount is the contract's,
    /// not something the player typed, which is why the manual builder still writes a zero there.
    /// </remarks>
    private WiredChestTransactionEntity NewContractTransaction(
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
            RoomEntityId = (int)RoomId,
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
                    _definitionProvider.TryGetDefinition(entity.FurnitureDefinitionEntityId)?.Name
                )
                .Where(name => !string.IsNullOrEmpty(name))
        );
}
