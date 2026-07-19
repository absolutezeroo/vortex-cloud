using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Inventory.Furniture;

public interface IFurnitureItem
{
    public RoomObjectId ItemId { get; }
    public PlayerId OwnerId { get; }
    public string OwnerName { get; }
    public FurnitureDefinitionSnapshot Definition { get; }
    public IExtraData ExtraData { get; }
    public IStuffData StuffData { get; }
    public FurnitureItemSnapshot GetSnapshot();
}
