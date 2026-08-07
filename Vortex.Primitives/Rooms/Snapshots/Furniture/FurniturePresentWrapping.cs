namespace Vortex.Primitives.Rooms.Snapshots.Furniture;

/// <summary>
/// A present's box and ribbon share one integer on the wire.
/// </summary>
/// <remarks>
/// <c>FurnitureGiftWrappedVisualization.updateTypes</c> reads the floor item's <c>extra</c> field
/// and splits it as <c>floor(extra / 1000)</c> and <c>extra % 1000</c>, one sprite layer each. The
/// same ×1000 packing the freeze blocks use for their state — it is a Habbo convention, not a
/// coincidence, so it is written down once here and shared by the room and the inventory.
/// </remarks>
public static class FurniturePresentWrapping
{
    private const int BoxMultiplier = 1000;

    public static int Pack(int boxType, int ribbonType) =>
        (boxType * BoxMultiplier) + (ribbonType % BoxMultiplier);

    public static (int BoxType, int RibbonType) Unpack(int packed) =>
        (packed / BoxMultiplier, packed % BoxMultiplier);
}
