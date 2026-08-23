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
/// The execution: every place a wallet and a chest move in the same breath.
/// </summary>
/// <remarks>
/// <see cref="WiredContractSettlement" /> decides what a contract takes and gives; this decides in
/// what order it happens and what is undone when a leg refuses. There are no distributed
/// transactions to be had -- the wallet grain is atomic per player and the room's context is atomic
/// per save -- so what stands in for one is ordering, and compensation on the far side of it.
/// <para>
/// The rule everything here follows: validate before anything moves, take payment before goods,
/// give the payment back if the goods cannot follow, and write the books last from what actually
/// landed. A crash between two legs still loses, but it loses in the direction that destroys
/// credits rather than the one that invents them.
/// </para>
/// </remarks>
public sealed partial class WiredTradeSettlement(
    RoomGrain roomGrain,
    RoomWiredTradingSystem system,
    WiredChestStore store,
    WiredChestLedger ledger
)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    private readonly RoomWiredTradingSystem _system = system;

    private readonly WiredChestStore _store = store;

    private readonly WiredChestLedger _ledger = ledger;

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
            .. rows.Select(entity => (entity, snapshot: _store.ToChestItemSnapshot(entity)))
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
    internal async Task<WiredDepositSnapshot?> SettleContractAsync(
        ActionContext ctx,
        WiredTradeSession session,
        CancellationToken ct
    )
    {
        // A settlement banks the stake in a chest and pays the reward out of one, so the box has to
        // have been pointed at a chest. Without that, the row this would otherwise create is keyed
        // to whatever furni came first — a contract, a lamp, or id 0 — and what is paid into it is
        // reachable by nobody.
        if (
            !_roomGrain._state.ItemsById.TryGetValue(session.ChestId, out IRoomItem? chestItem)
            || !WiredChestStore.IsChestLogic(chestItem.Definition.LogicName)
        )
        {
            _roomGrain._logger.LogWarning(
                "Contract settlement refused in room {RoomId}: {ChestId} is not a chest.",
                _roomGrain.RoomId,
                session.ChestId
            );

            return await SnapshotContractAsync(session, staked: null, completed: false, ct)
                .ConfigureAwait(true);
        }

        // Taken from the wallet before anything moves, so it has to go back if the move fails.
        int refundable = 0;

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity chest = await WiredChestStore
                .GetOrOpenAsync(dbCtx, session.ChestId, ct)
                .ConfigureAwait(true);

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
                session.Terms!,
                session.Multiplier,
                AsContractItems(staked)
            );

            if (
                payment is null
                || !WiredContractSettlement.TryReserveReward(
                    session.Terms!,
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
                _ledger.NewContractTransaction(
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

            // Closing the transaction is what forgets the session — one owner for it, so a
            // settlement cannot leave a screen behind by taking the wrong half down itself.
            await _system.CompleteTransactionAsync(ctx.PlayerId, ct).ConfigureAwait(true);

            await AnnounceContractSuccessAsync(ctx.PlayerId, session, ct).ConfigureAwait(true);

            // Both directions, not just `giving`: a contract that only takes payment moves rows OUT
            // of the player's inventory, and guarding on the receiving half alone left every
            // deposited item sitting in a cache that says it is still theirs. The grain's list is
            // built at activation and nothing reloads it on its own.
            if (giving.Count > 0 || paying.Count > 0)
            {
                await _roomGrain
                    ._grainFactory.GetInventoryGrain(ctx.PlayerId)
                    .ReloadFurnitureAsync(ct)
                    .ConfigureAwait(true);
            }

            await _store
                .ApplyChestSettingsToStuffDataAsync(session.ChestId, chest)
                .ConfigureAwait(true);

            await _system
                .NotifyOtherChestViewersAsync(
                    session.ChestId,
                    ctx.PlayerId,
                    new WiredChestItemsUpdateMessageComposer
                    {
                        ChestId = session.ChestId,
                        RemovedItemIds = reward.ItemIds,
                        AddedItems =
                        [
                            .. paying
                                .Select(_store.ToChestItemSnapshot)
                                .OfType<FurnitureItemSnapshot>(),
                        ],
                    }
                )
                .ConfigureAwait(true);

            return new WiredDepositSnapshot
            {
                ChestId = session.ChestId,
                Items =
                [
                    .. paying.Select(_store.ToChestItemSnapshot).OfType<FurnitureItemSnapshot>(),
                ],
                CanAccept = false,
                Completed = true,
                RewardItems =
                [
                    .. giving.Select(_store.ToChestItemSnapshot).OfType<FurnitureItemSnapshot>(),
                ],
                RewardCredits = paidOut,
            };
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to settle a contract against chest {ChestId} in room {RoomId}.",
                session.ChestId,
                _roomGrain.RoomId
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
                _roomGrain._logger.LogError(
                    "Player {PlayerId} paid {Coins} credits for a contract that failed to settle "
                        + "in room {RoomId}, and the refund was refused.",
                    ctx.PlayerId,
                    refundable,
                    _roomGrain.RoomId
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
        WiredTradeSession session,
        CancellationToken ct
    )
    {
        WiredContractSnapshot? written = await _roomGrain
            .ReadStoredContractAsync(session.ContractId, ct)
            .ConfigureAwait(true);

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(playerId)
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
    internal async Task<WiredDepositSnapshot?> SnapshotContractAsync(
        WiredTradeSession session,
        List<FurnitureEntity>? staked,
        bool completed,
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await WiredChestStore
                .ReadAsync(dbCtx, session.ChestId, ct)
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
                    session.Terms!,
                    session.Multiplier,
                    AsContractItems(staked)
                )
                is not null;

            bool chestCanPay = WiredContractSettlement.TryReserveReward(
                session.Terms!,
                session.Multiplier,
                AsContractItems(stock),
                chest?.Credits ?? 0,
                out ContractCharge reward
            );

            return new WiredDepositSnapshot
            {
                ChestId = session.ChestId,
                Items =
                [
                    .. staked.Select(_store.ToChestItemSnapshot).OfType<FurnitureItemSnapshot>(),
                ],
                CanAccept = priceMet && chestCanPay,
                Completed = completed,
                RewardItems =
                [
                    .. stock
                        .Where(row => reward.ItemIds.Contains(row.Id))
                        .Select(_store.ToChestItemSnapshot)
                        .OfType<FurnitureItemSnapshot>(),
                ],
                RewardCredits = reward.Coins,
            };
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to draw a contract table against chest {ChestId} in room {RoomId}.",
                session.ChestId,
                _roomGrain.RoomId
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
        WalletDebitResult result = await _roomGrain
            ._grainFactory.GetPlayerWalletGrain(playerId)
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
        _roomGrain
            ._grainFactory.GetPlayerWalletGrain(playerId)
            .GrantCurrencyAsync(
                new CurrencyKind { CurrencyType = CurrencyType.Credits },
                amount,
                ct
            );
}
