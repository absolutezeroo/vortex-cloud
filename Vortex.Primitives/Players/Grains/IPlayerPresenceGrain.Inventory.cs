using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Players.Grains;

public partial interface IPlayerPresenceGrain
{
    public Task OpenFurnitureInventoryAsync(CancellationToken ct);
    public Task OnFurnitureAddedAsync(FurnitureItemSnapshot snapshot, CancellationToken ct);
    public Task OnFurnitureRemovedAsync(RoomObjectId itemId, CancellationToken ct);

    /// <summary>
    /// Tells the client its furniture list is stale and to ask again. For the moves that rewrite
    /// rows in bulk rather than item by item -- a wired chest settlement shifts a whole stake and a
    /// whole reward in one transaction -- where naming every id would be a packet per item for no
    /// gain over one nudge.
    /// </summary>
    public Task OnFurnitureListInvalidatedAsync(CancellationToken ct);
    public Task OnBadgeGrantedAsync(string badgeCode, CancellationToken ct);
    public Task OnPetAddedToInventoryAsync(PetSnapshot pet, CancellationToken ct);
}
