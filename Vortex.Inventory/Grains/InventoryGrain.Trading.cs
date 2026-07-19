using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Inventory.Grains;

public sealed partial class InventoryGrain
{
    public async Task ApplyTradeResultAsync(
        IReadOnlyList<int> removedItemIds,
        IReadOnlyList<int> receivedItemIds,
        CancellationToken ct
    )
    {
        IPlayerPresenceGrain presence = _grainFactory.GetPlayerPresenceGrain(
            this.GetPrimaryKeyLong()
        );

        foreach (int itemId in removedItemIds)
        {
            await presence.OnFurnitureRemovedAsync(itemId, ct).ConfigureAwait(true);
        }

        // The ownership swap is already persisted; reload so given-away rows drop out and received
        // rows enter the cache with their real stuff data (the loader rebuilds it from the DB row).
        await _furniModule.ReloadAsync(ct).ConfigureAwait(true);

        foreach (int itemId in receivedItemIds)
        {
            FurnitureItemSnapshot? snapshot = await _furniModule
                .GetItemSnapshotAsync(itemId, ct)
                .ConfigureAwait(true);

            if (snapshot is not null)
            {
                await presence.OnFurnitureAddedAsync(snapshot, ct).ConfigureAwait(true);
            }
        }
    }
}
