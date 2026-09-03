using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Games.Freeze.Components;

/// <summary>
/// A Freeze exit tile (<c>es_exit</c>). Eliminated players are moved onto one of these, and a player
/// who walks onto it forfeits. Always walkable.
/// </summary>
[RoomObjectLogic("freeze_exit")]
public sealed class FreezeExitComponent(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : GameFurnitureLogic(stuffDataFactory, ctx), IArenaExitComponent
{
    public override GameId Game => FreezeConstants.Game;
}
