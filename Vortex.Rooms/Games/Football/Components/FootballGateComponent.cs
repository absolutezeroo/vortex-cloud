using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Games.Football.Components;

/// <summary>
/// A football team gate (<c>fball_gate_*</c>). Same rules as every other team gate — they are in
/// <c>TeamGateRules</c>, once, rather than copied per game.
/// </summary>
[RoomObjectLogic("football_gate_red")]
[RoomObjectLogic("football_gate_green")]
[RoomObjectLogic("football_gate_blue")]
[RoomObjectLogic("football_gate_yellow")]
public sealed class FootballGateComponent(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : GameFurnitureLogic(stuffDataFactory, ctx), ITeamGateComponent
{
    public override GameId Game => FootballConstants.Game;

    public GameTeamColor Team { get; } = GameColorKey.FromKeySuffix(ctx.Definition.LogicName);
}
