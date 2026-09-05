using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Games.Football.Components;

/// <summary>
/// A football (<c>fball</c>). Walking into it kicks it in the direction you were walking; where it
/// then goes is the game's ball simulation, never anything held here. It is walkable so a player can
/// step into it, and flat so they stand at floor level rather than on top of it.
/// <para>
/// The ball works with no match running, which is why it is not gated on one: a football placed in
/// an ordinary room is kickable, and the goals, scoreboards and timer are what turn that into a game.
/// </para>
/// <para>
/// <b>The key is the client's, not ours.</b> <c>furniture_pushable</c> is what the furnidata carries
/// for every <c>fball_ball*</c>, and it is the name the client resolves to its own
/// <c>FurniturePushableLogic</c>. A server-side name of our own invention (<c>football</c>,
/// <c>football_ball</c>) matches no definition row, so the ball binds to the default floor logic
/// instead: it never reports a walk-on, and the kick simply never happens. The same key covers the
/// pucks and yarn balls, which is correct — in Habbo every pushable furni is kicked the same way.
/// </para>
/// </summary>
[RoomObjectLogic("furniture_pushable")]
public sealed class FootballBallComponent(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : GameFurnitureLogic(stuffDataFactory, ctx), IBallComponent
{
    public override GameId Game => FootballConstants.Game;

    public override bool CanStack() => false;
}
