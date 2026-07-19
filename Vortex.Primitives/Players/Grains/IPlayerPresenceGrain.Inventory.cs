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
    public Task OnBadgeGrantedAsync(string badgeCode, CancellationToken ct);
    public Task OnPetAddedToInventoryAsync(PetSnapshot pet, CancellationToken ct);
}
