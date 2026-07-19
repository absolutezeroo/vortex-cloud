using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Inventory.Grains;

public partial interface IInventoryGrain
{
    /// <summary>Applies a committed trade to this player's live inventory: notifies the client of the
    /// items that left in the exchange, reloads owned furniture from the (already-updated) database
    /// so received items enter the cache with their real stuff data, then notifies the client of the
    /// items that arrived. Called after the atomic ownership swap has been persisted, so it is a
    /// pure cache/notification resync — safe and idempotent.</summary>
    public Task ApplyTradeResultAsync(
        IReadOnlyList<int> removedItemIds,
        IReadOnlyList<int> receivedItemIds,
        CancellationToken ct
    );
}
