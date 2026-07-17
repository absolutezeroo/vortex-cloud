using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Inventory.Snapshots;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Inventory.Grains;

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
