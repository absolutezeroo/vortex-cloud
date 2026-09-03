using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Games.BattleBanzai.Components;

/// <summary>
/// A Battle Banzai team gate (<c>bb_gate_*</c>). Walking onto it joins that team, or leaves it if
/// the player is already on it; during a match the gate is physically unwalkable (Arcturus
/// behaviour). One class claims all four colour keys via <see cref="GameColorKey"/> rather than
/// four shell subclasses; the state shows the team's member count, live display only.
/// </summary>
[RoomObjectLogic("battlebanzai_gate_red")]
[RoomObjectLogic("battlebanzai_gate_green")]
[RoomObjectLogic("battlebanzai_gate_blue")]
[RoomObjectLogic("battlebanzai_gate_yellow")]
public sealed class BanzaiGateComponent(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : GameFurnitureLogic(stuffDataFactory, ctx), ITeamGateComponent
{
    public override GameId Game => BanzaiConstants.Game;

    public GameTeamColor Team { get; } = GameColorKey.FromKeySuffix(ctx.Definition.LogicName);

    /// <summary>Walkability is precomputed into the tile flags, which is why the game recomputes
    /// every gate's tile when the match phase flips rather than this being asked per step.</summary>
    public override bool CanWalk() => !_ctx.Game.IsRunning(BanzaiConstants.Game);
}
