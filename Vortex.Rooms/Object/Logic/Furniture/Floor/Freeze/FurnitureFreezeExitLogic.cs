using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Freeze;

/// <summary>
/// A Freeze exit tile (the <c>es_exit</c> furni). Eliminated players are teleported onto one of these
/// (phase 2). Walkable so a player teleported here can walk away.
/// </summary>
[RoomObjectLogic("freeze_exit")]
public sealed class FurnitureFreezeExitLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    public override bool CanWalk() => true;
}
