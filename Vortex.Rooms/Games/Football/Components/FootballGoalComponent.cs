using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Games.Football.Components;

/// <summary>
/// A goal mouth (<c>fball_goal_*</c>). A ball entering it scores for the colour it carries. One class
/// claims every colour key; the colour resolves from the bound logic key, falling back to the
/// classname the way the scoreboards do.
/// </summary>
[RoomObjectLogic("football_goal_red")]
[RoomObjectLogic("football_goal_green")]
[RoomObjectLogic("football_goal_blue")]
[RoomObjectLogic("football_goal_yellow")]
public sealed class FootballGoalComponent(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : GameFurnitureLogic(stuffDataFactory, ctx), IGoalComponent
{
    public override GameId Game => FootballConstants.Game;

    public GameTeamColor Team { get; } =
        GameColorKey.FromKeySuffix(ctx.Definition.LogicName) is var byKey
        && byKey != GameTeamColor.None
            ? byKey
            : GameColorKey.FromKeySuffix(ctx.Definition.Name);

    public Rotation Facing => _ctx.RoomObject.Rotation;
}
