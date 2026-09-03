using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Games.Freeze.Components;

/// <summary>
/// A Freeze arena tile (<c>es_tile</c>). The play area is simply the set of these placed in the room
/// — there is no bounding box. Players stand on them and throw a snowball by double-clicking, which
/// arrives as a use signal and is an intent the game decides on. Its animation state (rise, blast,
/// reset) is ephemeral display driven by <see cref="FreezeGame"/>.
/// </summary>
[RoomObjectLogic("freeze_tile")]
public sealed class FreezeTileComponent(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : GameFurnitureLogic(stuffDataFactory, ctx), IArenaTileComponent
{
    public override GameId Game => FreezeConstants.Game;
}
