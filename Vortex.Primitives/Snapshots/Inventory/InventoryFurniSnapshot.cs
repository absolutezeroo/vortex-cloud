using Vortex.Primitives.Players;

namespace Vortex.Primitives.Snapshots.Inventory;

public sealed record InventoryFurniSnapshot(
    int Id,
    PlayerId PlayerId,
    int DefinitionId,
    string? StuffData
);
