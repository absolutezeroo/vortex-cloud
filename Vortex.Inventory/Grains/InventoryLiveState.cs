using System.Collections.Generic;
using Vortex.Primitives.Inventory.Furniture;

namespace Vortex.Inventory.Grains;

internal sealed class InventoryLiveState
{
    public Dictionary<int, IFurnitureItem> FurnitureById { get; } = [];
    public bool IsFurnitureReady { get; set; } = false;
}
