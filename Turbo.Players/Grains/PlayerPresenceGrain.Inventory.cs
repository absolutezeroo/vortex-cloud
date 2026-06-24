using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Inventory.Snapshots;
using Turbo.Primitives.Pets.Snapshots;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Players.Grains;

internal sealed partial class PlayerPresenceGrain
{
    public Task OpenFurnitureInventoryAsync(CancellationToken ct) =>
        _inventoryModule.OpenFurnitureInventoryAsync(ct);

    public Task OnFurnitureAddedAsync(FurnitureItemSnapshot snapshot, CancellationToken ct) =>
        _inventoryModule.OnFurnitureAddedAsync(snapshot, ct);

    public Task OnFurnitureRemovedAsync(RoomObjectId itemId, CancellationToken ct) =>
        _inventoryModule.OnFurnitureRemovedAsync(itemId, ct);

    public Task OnBadgeGrantedAsync(string badgeCode, CancellationToken ct) =>
        _inventoryModule.OnBadgeGrantedAsync(badgeCode, ct);

    public Task OnPetAddedToInventoryAsync(PetSnapshot pet, CancellationToken ct) =>
        _inventoryModule.OnPetAddedToInventoryAsync(pet, ct);
}
