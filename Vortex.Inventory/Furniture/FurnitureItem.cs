using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Inventory.Furniture;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Inventory.Furniture;

internal sealed class FurnitureItem : IFurnitureItem
{
    public required RoomObjectId ItemId { get; init; }
    public required PlayerId OwnerId { get; init; }
    public required string OwnerName { get; init; } = string.Empty;
    public required FurnitureDefinitionSnapshot Definition { get; init; }
    public required IExtraData ExtraData { get; init; }
    public required IStuffData StuffData { get; init; }

    private FurnitureItemSnapshot? _snapshot;

    public FurnitureItemSnapshot GetSnapshot()
    {
        if (_snapshot is null)
        {
            _snapshot = BuildSnapshot();
        }

        return _snapshot;
    }

    private FurnitureItemSnapshot BuildSnapshot() =>
        new()
        {
            ItemId = ItemId,
            SpriteId = Definition.SpriteId,
            OwnerId = OwnerId,
            OwnerName = OwnerName,
            Definition = Definition,
            StuffData = StuffData.GetSnapshot(),
            ExtraData = ExtraData.GetJsonString(),
            SecondsToExpiration = -1,
            HasRentPeriodStarted = false,
            RoomId = -1,
        };
}
