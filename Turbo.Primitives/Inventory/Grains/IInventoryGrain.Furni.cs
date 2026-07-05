using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Inventory.Furniture;
using Turbo.Primitives.Inventory.Snapshots;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Snapshots.Furniture;

namespace Turbo.Primitives.Inventory.Grains;

public partial interface IInventoryGrain
{
    public Task<bool> AddFurnitureAsync(IFurnitureItem item, CancellationToken ct);
    public Task<bool> AddFurnitureFromRoomItemSnapshotAsync(
        RoomItemSnapshot snapshot,
        CancellationToken ct
    );
    public Task<bool> RemoveFurnitureAsync(RoomObjectId itemId, CancellationToken ct);
    public Task GrantCatalogOfferAsync(
        CatalogOfferSnapshot offer,
        string extraParam,
        int quantity,
        CancellationToken ct
    );
    public Task<FurnitureItemSnapshot?> GetItemSnapshotAsync(
        RoomObjectId itemId,
        CancellationToken ct
    );
    public Task<ImmutableArray<FurnitureItemSnapshot>> GetAllItemSnapshotsAsync(
        CancellationToken ct
    );

    public Task GrantBadgeAsync(string badgeCode, CancellationToken ct);

    public Task GrantFurnitureDefinitionAsync(
        int definitionId,
        string? extraData,
        CancellationToken ct
    );
    public Task GrantLtdFurnitureAsync(
        int furniDefinitionId,
        int serialNumber,
        int seriesSize,
        CancellationToken ct
    );

    /// <summary>Creates exactly one furniture item, but only if the player's current owned-furniture
    /// count is below <paramref name="furniLimit"/>; returns null (nothing created) otherwise. The
    /// check-then-create is atomic because Orleans serializes calls to this grain instance -- no
    /// separate reservation/release step is needed, unlike a non-actor-model server would require.
    /// </summary>
    public Task<FurnitureItemSnapshot?> GrantSingleFurnitureIfUnderLimitAsync(
        int definitionId,
        string? extraData,
        int furniLimit,
        CancellationToken ct
    );
}
