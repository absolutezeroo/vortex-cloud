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
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
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
/// Wired trading: chests, the contracts that sell out of them, and the trade screen both use.
/// </summary>
/// <remarks>
/// Kept on the room grain rather than in a grain of its own for the same reason the player trade is
/// (<see cref="RoomTradingSystem" />): every step needs the room's live objects and avatars, and a
/// leaving player's session is torn down inside the same turn that removes their avatar.
/// <para>
/// This owns the volatile half -- who has a screen open, who has been offered what, and until when.
/// None of it is persisted on purpose: a trade screen is a UI session, nothing of value moves before
/// the final confirm, and a silo that goes away mid-session costs a screen the client closes on its
/// own timer anyway. What is worth keeping lives in <see cref="WiredChestStore" /> and
/// <see cref="WiredChestLedger" />.
/// </para>
/// </remarks>
public sealed partial class RoomWiredTradingSystem
{
    private readonly RoomGrain _roomGrain;

    internal readonly WiredChestStore Store;

    internal readonly WiredChestLedger Ledger;

    internal readonly WiredTradeSettlement Settlement;

    public RoomWiredTradingSystem(RoomGrain roomGrain)
    {
        _roomGrain = roomGrain;

        Store = new WiredChestStore(roomGrain, this);
        Ledger = new WiredChestLedger(roomGrain, Store);
        Settlement = new WiredTradeSettlement(roomGrain, this, Store, Ledger);
    }

    private WiredChestStore _store => Store;

    private WiredChestLedger _ledger => Ledger;

    private WiredTradeSettlement _settlement => Settlement;

    /// <summary>
    /// Every open trade screen, by the player looking at it.
    /// </summary>
    /// <remarks>
    /// Keyed by player rather than by chest: a player can only have one trade screen up, while a
    /// chest can be filled by several people at once.
    /// <para>
    /// A plain deposit and a contract offer are one dictionary because they are one screen -- the
    /// client drives both with the same three messages. They used to be two, and the two could
    /// disagree: the cleanup that ran when a player walked out forgot the offer half, so the stack
    /// waiting on <c>wf_trg_transaction_failed</c> never heard about a leaver.
    /// </para>
    /// </remarks>
    internal readonly Dictionary<PlayerId, WiredTradeSession> _sessions = [];

    /// <summary>
    /// The requirement type that lets any tradeable furni be staked.
    /// </summary>
    /// <remarks>
    /// The client's <c>WiredTradeRequirementsModel.canOfferFurni()</c> branches on this: 0 takes
    /// only credit furniture, 1 refuses it, 2 takes anything, 4 reads a rules block. A chest takes
    /// what it is given, so 2 — and with no rules block to write, none is sent.
    /// </remarks>
    private const int DepositRequirementAnyFurni = 2;

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
        if (
            ctx.PlayerId <= 0
            || !_roomGrain._state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
        )
        {
            _roomGrain._logger.LogDebug(
                "Deposit refused: {ChestId} is not an item of room {RoomId}.",
                chestId,
                _roomGrain.RoomId
            );

            return WiredDepositStart.Refused;
        }

        // Refusing tells the client nothing — it simply waits for a trade that never opens — so
        // each gate says which one it was. A refusal is ordinary, hence debug rather than warn.
        if (!WiredChestStore.IsChestLogic(item.Definition.LogicName))
        {
            _roomGrain._logger.LogDebug(
                "Deposit refused: {ChestId} has logic {Logic}, which is not a chest.",
                chestId,
                item.Definition.LogicName
            );

            return WiredDepositStart.Refused;
        }

        if (WiredChestStore.IsCoinChestLogic(item.Definition.LogicName))
        {
            _roomGrain._logger.LogDebug(
                "Deposit refused: {ChestId} is a coin chest, and the client cannot name an amount.",
                chestId
            );

            return WiredDepositStart.Refused;
        }

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await WiredChestStore
                .ReadAsync(dbCtx, chestId, ct)
                .ConfigureAwait(true);

            if (chest is not null && chest.Locked)
            {
                _roomGrain._logger.LogDebug("Deposit refused: chest {ChestId} is locked.", chestId);

                return WiredDepositStart.Refused;
            }

            if (chest is null || !chest.EveryoneCanDonate)
            {
                RoomControllerType level = await _roomGrain
                    .SecurityModule.GetControllerLevelAsync(ctx)
                    .ConfigureAwait(true);

                if (level == RoomControllerType.None)
                {
                    _roomGrain._logger.LogDebug(
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
            bool replaced = _sessions.ContainsKey(ctx.PlayerId);

            _sessions[ctx.PlayerId] = new WiredTradeSession(chestId, []);

            return replaced ? WiredDepositStart.Replaced : WiredDepositStart.Opened;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to open a deposit for wired chest {ChestId} in room {RoomId}.",
                chestId,
                _roomGrain.RoomId
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
        // An offer that ran out of time takes its screen with it, so a client that ignored its own
        // timer finds no session here rather than settling on a price that expired.
        await ExpireTimedOutTransactionsAsync(ct).ConfigureAwait(true);

        if (!_sessions.TryGetValue(ctx.PlayerId, out WiredTradeSession? deposit))
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
                    int id in await _store
                        .ReadDepositableIdsAsync(ctx, itemIds, ct)
                        .ConfigureAwait(true)
                )
                {
                    deposit.ItemIds.Add(id);
                }
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Failed to stake items for a deposit in room {RoomId}.",
                    _roomGrain.RoomId
                );

                return null;
            }
        }

        return deposit.Terms is null
            ? await SnapshotDepositAsync(deposit, completed: false, ct).ConfigureAwait(true)
            : await _settlement
                .SnapshotContractAsync(deposit, staked: null, completed: false, ct)
                .ConfigureAwait(true);
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
        // An offer that ran out of time takes its screen with it, so a client that ignored its own
        // timer finds no session here rather than settling on a price that expired.
        await ExpireTimedOutTransactionsAsync(ct).ConfigureAwait(true);

        if (!_sessions.TryGetValue(ctx.PlayerId, out WiredTradeSession? deposit))
        {
            return null;
        }

        if (deposit.Terms is not null)
        {
            // A contract's table is drawn from both sides, and only its own accept may settle it.
            return confirm
                ? await _settlement.SettleContractAsync(ctx, deposit, ct).ConfigureAwait(true)
                : await _settlement
                    .SnapshotContractAsync(deposit, staked: null, completed: false, ct)
                    .ConfigureAwait(true);
        }

        if (!confirm)
        {
            return await SnapshotDepositAsync(deposit, completed: false, ct).ConfigureAwait(true);
        }

        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity chest = await WiredChestStore
                .GetOrOpenAsync(dbCtx, deposit.ChestId, ct)
                .ConfigureAwait(true);

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
                    _ledger.NewManualTransaction(
                        ctx.PlayerId,
                        chest.Id,
                        depositFurni: moving.Count,
                        definitionInfo: string.Join(
                            ", ",
                            moving
                                .Select(entity =>
                                    _roomGrain
                                        ._definitionProvider.TryGetDefinition(
                                            entity.FurnitureDefinitionEntityId
                                        )
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
                Items =
                [
                    .. moving.Select(_store.ToChestItemSnapshot).OfType<FurnitureItemSnapshot>(),
                ],
                CanAccept = false,
                Completed = true,
            };

            _sessions.Remove(ctx.PlayerId);

            // What the chest floats above itself is drawn from what it holds, which just changed.
            await _store
                .ApplyChestSettingsToStuffDataAsync(deposit.ChestId, chest)
                .ConfigureAwait(true);

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
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to complete a deposit into wired chest {ChestId} in room {RoomId}.",
                deposit.ChestId,
                _roomGrain.RoomId
            );

            return null;
        }
    }

    /// <summary>
    /// Drops an open trade. Nothing moved, so there is nothing to undo.
    /// </summary>
    /// <remarks>
    /// A contract turned down is a transaction that failed, and the wiring is listening for that —
    /// so the offer goes with the screen rather than sitting there until it times out.
    /// </remarks>
    public async Task<bool> CancelWiredDepositAsync(ActionContext ctx, CancellationToken ct)
    {
        if (!_sessions.Remove(ctx.PlayerId, out WiredTradeSession? cancelled))
        {
            return false;
        }

        if (cancelled.IsOffer)
        {
            await RaiseTransactionFailedAsync(ctx.PlayerId, ct).ConfigureAwait(true);
        }

        return true;
    }

    /// <summary>Reads the staked rows back out so the table can be redrawn from the database.</summary>
    private async Task<WiredDepositSnapshot?> SnapshotDepositAsync(
        WiredTradeSession deposit,
        bool completed,
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<FurnitureEntity> rows = await dbCtx
                .Furnitures.AsNoTracking()
                .Where(f => deposit.ItemIds.Contains(f.Id) && f.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            return new WiredDepositSnapshot
            {
                ChestId = deposit.ChestId,
                Items =
                [
                    .. rows.Select(_store.ToChestItemSnapshot).OfType<FurnitureItemSnapshot>(),
                ],
                CanAccept = deposit.ItemIds.Count > 0,
                Completed = completed,
            };
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to read a deposit's table in room {RoomId}.",
                _roomGrain.RoomId
            );

            return null;
        }
    }

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
        int mode,
        int multiplier,
        int timeoutSeconds
    ) =>
        _sessions[playerId] = new WiredTradeSession(chestId, [])
        {
            Terms = contract,
            ContractId = contractId,
            Mode = mode,
            Multiplier = Math.Max(1, multiplier),
            ExpiresAt = timeoutSeconds > 0 ? DateTime.UtcNow.AddSeconds(timeoutSeconds) : null,
        };

    /// <summary>
    /// Drops anything that has run out of time, and says who was dropped.
    /// </summary>
    /// <remarks>
    /// Checked whenever a session is touched rather than on the room clock. A timeout only matters
    /// at the moment someone asks about it, and a tick that exists to notice nothing most of the
    /// time is a tick not worth paying for.
    /// <para>
    /// Only an offer carries a deadline, so a plain deposit is never swept: the screen it opened
    /// belongs to the player, not to a box that is still waiting.
    /// </para>
    /// </remarks>
    private List<PlayerId> ExpireTimedOutTransactions()
    {
        DateTime now = DateTime.UtcNow;

        List<PlayerId> expired =
        [
            .. _sessions
                .Where(entry => entry.Value.ExpiresAt is { } deadline && deadline <= now)
                .Select(entry => entry.Key),
        ];

        foreach (PlayerId playerId in expired)
        {
            // The screen and the offer go together: a trade nobody is still being offered must not
            // settle, and they were two removals when they were two dictionaries.
            _sessions.Remove(playerId);
        }

        return expired;
    }

    /// <summary>Drops what has run out of time, and tells the stacks that were waiting on it.</summary>
    /// <remarks>
    /// Runs at the top of everything that touches a transaction — the offer, the cancel, and the
    /// two the trading screen drives. An expired offer that can still be accepted is a price with
    /// no deadline, and the deadline is the box's own setting.
    /// </remarks>
    private async Task ExpireTimedOutTransactionsAsync(CancellationToken ct)
    {
        foreach (PlayerId timedOut in ExpireTimedOutTransactions())
        {
            await RaiseTransactionFailedAsync(timedOut, ct).ConfigureAwait(true);
        }
    }

    private Task RaiseTransactionFailedAsync(PlayerId playerId, CancellationToken ct) =>
        _roomGrain.PublishRoomEventAsync(
            new WiredTransactionFailedEvent
            {
                RoomId = _roomGrain._state.RoomId,
                CausedBy = ActionContext.CreateForPlayer(playerId, _roomGrain.RoomId),
                PlayerId = playerId,
            },
            ct
        );

    public async Task<bool> OfferTransactionAsync(
        int contractId,
        PlayerId playerId,
        int chestId,
        TradeContract? contract,
        int mode,
        int multiplier,
        int timeoutSeconds,
        CancellationToken ct
    )
    {
        if (
            contractId <= 0
            || playerId <= 0
            || !_roomGrain._state.ItemsById.ContainsKey(contractId)
        )
        {
            return false;
        }

        await ExpireTimedOutTransactionsAsync(ct).ConfigureAwait(true);

        WiredContractSnapshot? written = await _roomGrain
            .ReadStoredContractAsync(contractId, ct)
            .ConfigureAwait(true);

        // What the contract itself says, if anyone has written it; the add-on otherwise.
        TradeContract? terms =
            written is { } stored
            && (stored.YouGiveRules is not null || stored.YouGetRule is not null)
                ? new TradeContract
                {
                    YouGiveRules = stored.YouGiveRules,
                    YouGetRule = stored.YouGetRule,
                    Mode = mode,
                    Multiplier = Math.Max(1, multiplier),
                    AutoMultiplierMax = Math.Max(1, multiplier),
                }
                : contract;

        if (terms is null)
        {
            _roomGrain._logger.LogWarning(
                "Contract {ContractId} in room {RoomId} states no terms and the box carries no "
                    + "custom-contract add-on, so no offer was made.",
                contractId,
                _roomGrain.RoomId
            );

            return false;
        }

        // Withdrawing the previous offer happens here rather than above, because an offer that is
        // never made withdraws nothing: refusing for want of terms used to leave the player's screen
        // already taken down. A plain deposit is replaced without a failure — it was never an offer.
        if (_sessions.TryGetValue(playerId, out WiredTradeSession? standing) && standing.IsOffer)
        {
            _sessions.Remove(playerId);

            await RaiseTransactionFailedAsync(playerId, ct).ConfigureAwait(true);
        }

        // The screen the offer opens is the one the settlement runs on, so it exists before the
        // player can put anything on it.
        OpenContractSession(playerId, contractId, chestId, terms, mode, multiplier, timeoutSeconds);

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(playerId)
            .SendComposerAsync(
                new WiredTradeInitiateMessageComposer
                {
                    RequirementType = CustomRequirement,
                    // A payment contract names both of these in its own editor; anything else
                    // leaves the screen to its defaults.
                    YouGetText = written?.ReceiveText ?? string.Empty,
                    LayoutType = written?.LayoutType ?? string.Empty,
                    // A price is the whole point of a contract, so it is on screen before the
                    // player puts anything up rather than after.
                    ShowRequirementsImmediate = true,
                    OverridePreviousTrade = true,
                    TimeoutSeconds = timeoutSeconds,
                    Contract = terms,
                }
            )
            .ConfigureAwait(true);

        _roomGrain._logger.LogDebug(
            "Contract {ContractId} offered to player {PlayerId} in room {RoomId}.",
            contractId,
            playerId,
            _roomGrain.RoomId
        );

        return true;
    }

    /// <summary>The requirement type whose payload carries terms; the other three carry none.</summary>
    private const int CustomRequirement = 4;

    public async Task<int> CancelTransactionAsync(
        int contractId,
        PlayerId playerId,
        CancellationToken ct
    )
    {
        await ExpireTimedOutTransactionsAsync(ct).ConfigureAwait(true);

        if (playerId <= 0 || !_sessions.TryGetValue(playerId, out WiredTradeSession? session))
        {
            return 0;
        }

        // A plain deposit is not a transaction and cancelling one is not this message's business:
        // the screen it opened is the player's, and CancelWiredDepositAsync is what closes it.
        if (!session.IsOffer)
        {
            return 0;
        }

        // contractId 0 is the client's "any ongoing transaction"; anything else has to match.
        if (contractId > 0 && session.ContractId != contractId)
        {
            return 0;
        }

        _sessions.Remove(playerId);

        await RaiseTransactionFailedAsync(playerId, ct).ConfigureAwait(true);

        return 1;
    }

    /// <summary>
    /// Closes a transaction as done and raises the trigger that waits on it.
    /// </summary>
    /// <remarks>
    /// Called once both sides of the contract have actually moved — see
    /// <c>_settlement.SettleContractAsync</c>. Nothing else raises the completion trigger, which is the point
    /// of it being here rather than at the settlement's own end.
    /// </remarks>
    internal async Task<bool> CompleteTransactionAsync(PlayerId playerId, CancellationToken ct)
    {
        // The session goes here and only here on the winning path: the settlement used to drop it
        // first and then ask for the trigger, which now would be asking about a session it had
        // already forgotten.
        if (!_sessions.TryGetValue(playerId, out WiredTradeSession? session) || !session.IsOffer)
        {
            return false;
        }

        _sessions.Remove(playerId);

        await _roomGrain
            .PublishRoomEventAsync(
                new WiredTransactionCompletedEvent
                {
                    RoomId = _roomGrain._state.RoomId,
                    CausedBy = ActionContext.CreateForPlayer(playerId, _roomGrain.RoomId),
                    PlayerId = playerId,
                },
                ct
            )
            .ConfigureAwait(true);

        return true;
    }
}
