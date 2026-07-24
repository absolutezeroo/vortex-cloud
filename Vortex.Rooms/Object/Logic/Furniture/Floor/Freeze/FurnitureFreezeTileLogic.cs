using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Freeze;

/// <summary>
/// A Freeze arena tile (the <c>es_tile</c> furni). The play area is simply the set of these placed in
/// the room — there is no bounding box. Players stand on them and throw by double-clicking (wired in
/// phase 2 via <c>OnUseAsync</c>). Walkable so the arena can be traversed.
/// </summary>
[RoomObjectLogic("freeze_tile")]
public sealed class FurnitureFreezeTileLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    public override bool CanWalk() => true;
}
