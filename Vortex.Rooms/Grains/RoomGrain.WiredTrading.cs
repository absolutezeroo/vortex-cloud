using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Rooms.Grains;

/// <summary>
/// The grain's wired-trading surface, which is delegation and nothing else.
/// </summary>
/// <remarks>
/// Chests, contracts and the trade screen live in <c>RoomWiredTradingSystem</c> and the three
/// components under it, the same way the player trade lives in <c>RoomTradingSystem</c> behind
/// <c>RoomGrain.Trading.cs</c>. Every signature here is the one the interface already declared:
/// the handlers, the composers and the wire are untouched by the move.
/// </remarks>
public sealed partial class RoomGrain
{
    public Task<WiredChestSnapshot?> OpenWiredChestAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    ) => WiredTradingSystem.OpenWiredChestAsync(ctx, chestId, ct);

    public Task CloseWiredChestAsync(ActionContext ctx, int chestId, CancellationToken ct) =>
        WiredTradingSystem.CloseWiredChestAsync(ctx, chestId, ct);

    public Task<ImmutableArray<FurnitureItemSnapshot>?> ListWiredChestItemsAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    ) => WiredTradingSystem.ListWiredChestItemsAsync(ctx, chestId, ct);

    public Task<WiredDepositStart> StartWiredChestDepositAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    ) => WiredTradingSystem.StartWiredChestDepositAsync(ctx, chestId, ct);

    public Task<WiredDepositSnapshot?> UpdateWiredDepositItemsAsync(
        ActionContext ctx,
        bool remove,
        ImmutableArray<int> itemIds,
        CancellationToken ct
    ) => WiredTradingSystem.UpdateWiredDepositItemsAsync(ctx, remove, itemIds, ct);

    public Task<WiredDepositSnapshot?> AcceptWiredDepositAsync(
        ActionContext ctx,
        bool confirm,
        CancellationToken ct
    ) => WiredTradingSystem.AcceptWiredDepositAsync(ctx, confirm, ct);

    public Task<bool> CancelWiredDepositAsync(ActionContext ctx, CancellationToken ct) =>
        WiredTradingSystem.CancelWiredDepositAsync(ctx, ct);

    public Task<WiredChestSnapshot?> WithdrawWiredChestCreditsAsync(
        ActionContext ctx,
        int chestId,
        int amount,
        CancellationToken ct
    ) => WiredTradingSystem.Settlement.WithdrawWiredChestCreditsAsync(ctx, chestId, amount, ct);

    public Task<ImmutableArray<int>> WithdrawWiredChestItemsAsync(
        ActionContext ctx,
        int chestId,
        bool isWallItem,
        int typeId,
        string legacyPosterId,
        int count,
        CancellationToken ct
    ) =>
        WiredTradingSystem.Settlement.WithdrawWiredChestItemsAsync(
            ctx,
            chestId,
            isWallItem,
            typeId,
            legacyPosterId,
            count,
            ct
        );

    public Task<int> PayOutWiredChestCreditsAsync(
        int chestId,
        PlayerId playerId,
        int amount,
        bool everything,
        CancellationToken ct
    ) =>
        WiredTradingSystem.Settlement.PayOutWiredChestCreditsAsync(
            chestId,
            playerId,
            amount,
            everything,
            ct
        );

    public Task<int> PayOutWiredChestItemsAsync(
        int chestId,
        PlayerId playerId,
        int count,
        CancellationToken ct
    ) => WiredTradingSystem.Settlement.PayOutWiredChestItemsAsync(chestId, playerId, count, ct);

    public Task SaveWiredChestSettingsAsync(
        ActionContext ctx,
        int chestId,
        string name,
        string description,
        bool everyoneCanOpen,
        bool everyoneCanDonate,
        int chestState,
        int previewItems,
        int previewAmount,
        CancellationToken ct
    ) =>
        WiredTradingSystem.SaveWiredChestSettingsAsync(
            ctx,
            chestId,
            name,
            description,
            everyoneCanOpen,
            everyoneCanDonate,
            chestState,
            previewItems,
            previewAmount,
            ct
        );

    public Task SaveWiredChestNotificationSettingsAsync(
        ActionContext ctx,
        int chestId,
        int notificationMode,
        bool notifyWhenFull,
        bool notifyOnDonation,
        bool notifyOnWithdraw,
        bool notifyWhenEmpty,
        bool notifyOnAnyWiredTransaction,
        CancellationToken ct
    ) =>
        WiredTradingSystem.SaveWiredChestNotificationSettingsAsync(
            ctx,
            chestId,
            notificationMode,
            notifyWhenFull,
            notifyOnDonation,
            notifyOnWithdraw,
            notifyWhenEmpty,
            notifyOnAnyWiredTransaction,
            ct
        );

    public Task SetWiredChestLockAsync(
        ActionContext ctx,
        int chestId,
        bool locked,
        bool autoLock,
        CancellationToken ct
    ) => WiredTradingSystem.SetWiredChestLockAsync(ctx, chestId, locked, autoLock, ct);

    public Task SetAllWiredChestLocksAsync(ActionContext ctx, bool locked, CancellationToken ct) =>
        WiredTradingSystem.SetAllWiredChestLocksAsync(ctx, locked, ct);

    public Task<WiredTransactionsSnapshot?> GetWiredChestTransactionsAsync(
        ActionContext ctx,
        int chestId,
        int pageSize,
        int page,
        CancellationToken ct
    ) => WiredTradingSystem.Ledger.GetWiredChestTransactionsAsync(ctx, chestId, pageSize, page, ct);

    public Task<WiredTransactionsSnapshot?> GetWiredRoomTransactionsAsync(
        ActionContext ctx,
        int pageSize,
        int page,
        CancellationToken ct
    ) => WiredTradingSystem.Ledger.GetWiredRoomTransactionsAsync(ctx, pageSize, page, ct);

    public Task<WiredTransactionDetailsSnapshot?> GetWiredTransactionDetailsAsync(
        ActionContext ctx,
        long transactionId,
        CancellationToken ct
    ) => WiredTradingSystem.Ledger.GetWiredTransactionDetailsAsync(ctx, transactionId, ct);

    public Task<bool> OfferTransactionAsync(
        int contractId,
        PlayerId playerId,
        int chestId,
        TradeContract? contract,
        int mode,
        int multiplier,
        int timeoutSeconds,
        CancellationToken ct
    ) =>
        WiredTradingSystem.OfferTransactionAsync(
            contractId,
            playerId,
            chestId,
            contract,
            mode,
            multiplier,
            timeoutSeconds,
            ct
        );

    public Task<int> CancelTransactionAsync(
        int contractId,
        PlayerId playerId,
        CancellationToken ct
    ) => WiredTradingSystem.CancelTransactionAsync(contractId, playerId, ct);

    /// <summary>Tears down the trade screen and the offer of a leaving player, and shuts the lids
    /// nobody is left looking into. Called from <c>RoomGrain.Avatar.cs</c> when an avatar is
    /// removed.</summary>
    internal Task CloseChestScreensForLeavingPlayerAsync(PlayerId playerId) =>
        WiredTradingSystem.HandlePlayerLeftAsync(playerId);
}
