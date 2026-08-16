using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
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
    Task<WiredChestSnapshot?> WithdrawWiredChestCreditsAsync(
        ActionContext ctx,
        int chestId,
        int amount,
        CancellationToken ct
    );
}
