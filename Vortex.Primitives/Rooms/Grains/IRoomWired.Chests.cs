using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomWired
{
    /// <summary>
    /// Opens a wired chest for whoever asked, if they are allowed to and if the id really is a chest
    /// standing in this room. Null when it is not.
    /// </summary>
    /// <remarks>
    /// The chest's own row is created on first open rather than when the furni is placed: a chest
    /// nobody has ever touched holds nothing, and a row saying so is a row to keep in step for
    /// nothing.
    /// </remarks>
    Task<WiredChestSnapshot?> OpenWiredChestAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    );

    /// <summary>
    /// Moves credits out of a chest and into the asking player's wallet. Returns what the chest
    /// holds afterwards, or null when nothing moved.
    /// </summary>
    /// <remarks>
    /// Pass <paramref name="amount"/> as 0 or less to take everything, which is what the chest's
    /// "withdraw all" button asks for.
    /// </remarks>
    /// <summary>
    /// Everything a furniture chest holds, for the screen the client opens.
    /// </summary>
    /// <remarks>
    /// Empty is a real answer, not a failure: the client needs a page even for an empty chest or the
    /// screen never opens. Null means the caller had no business asking.
    /// </remarks>
    Task<ImmutableArray<FurnitureItemSnapshot>?> ListWiredChestItemsAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    );

    /// <summary>
    /// Takes items of one kind out of a chest and hands them to whoever asked.
    /// </summary>
    /// <remarks>
    /// The chest screen groups identical furni, so the request names a kind and a count rather than
    /// ids; which of the matching items leave is this method's choice. Returns the ids that left, so
    /// the caller can tell the client what to remove from a screen it already drew.
    /// </remarks>
    /// <summary>
    /// Locks or unlocks every chest in this room at once.
    /// </summary>
    Task SetAllWiredChestLocksAsync(ActionContext ctx, bool locked, CancellationToken ct);

    /// <summary>
    /// One chest's transaction log, a page at a time.
    /// </summary>
    /// <remarks>
    /// The page carries its own list type and id, because the client re-requests later pages with
    /// what it reads back rather than with what it asked for.
    /// </remarks>
    Task<WiredTransactionsSnapshot?> GetWiredChestTransactionsAsync(
        ActionContext ctx,
        int chestId,
        int pageSize,
        int page,
        CancellationToken ct
    );

    /// <summary>
    /// Every chest in this room, a page at a time.
    /// </summary>
    Task<WiredTransactionsSnapshot?> GetWiredRoomTransactionsAsync(
        ActionContext ctx,
        int pageSize,
        int page,
        CancellationToken ct
    );

    /// <summary>
    /// Saves the chest's settings dialog: its name, its description, who may open and donate, and
    /// how it looks when open.
    /// </summary>
    Task SaveWiredChestSettingsAsync(
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
    );

    /// <summary>
    /// Saves when and about what the chest notifies its owner.
    /// </summary>
    Task SaveWiredChestNotificationSettingsAsync(
        ActionContext ctx,
        int chestId,
        int notificationMode,
        bool notifyWhenFull,
        bool notifyOnDonation,
        bool notifyOnWithdraw,
        bool notifyWhenEmpty,
        bool notifyOnAnyWiredTransaction,
        CancellationToken ct
    );

    /// <summary>
    /// Locks a chest, or has it lock itself again after use.
    /// </summary>
    /// <remarks>
    /// Capacity is deliberately not a parameter: the client sends one and it is not ours to take.
    /// </remarks>
    Task SetWiredChestLockAsync(
        ActionContext ctx,
        int chestId,
        bool locked,
        bool autoLock,
        CancellationToken ct
    );

    Task<ImmutableArray<int>> WithdrawWiredChestItemsAsync(
        ActionContext ctx,
        int chestId,
        bool isWallItem,
        int typeId,
        string legacyPosterId,
        int count,
        CancellationToken ct
    );

    Task<WiredChestSnapshot?> WithdrawWiredChestCreditsAsync(
        ActionContext ctx,
        int chestId,
        int amount,
        CancellationToken ct
    );
}
