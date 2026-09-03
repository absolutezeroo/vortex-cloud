using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Games.Freeze.Components;

/// <summary>
/// A Freeze team gate (<c>es_gate_*</c>). Walking onto it joins that team, or leaves it if the
/// player is already on it. Unlike the Banzai gate it stays walkable during a match — the Freeze
/// arena is entered and left on foot — and its state shows the team's living member count.
/// </summary>
[RoomObjectLogic("freeze_gate_red")]
[RoomObjectLogic("freeze_gate_green")]
[RoomObjectLogic("freeze_gate_blue")]
[RoomObjectLogic("freeze_gate_yellow")]
public sealed class FreezeGateComponent(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : GameFurnitureLogic(stuffDataFactory, ctx), ITeamGateComponent
{
    public override GameId Game => FreezeConstants.Game;

    public GameTeamColor Team { get; } = GameColorKey.FromKeySuffix(ctx.Definition.LogicName);
}
