using System;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Games.BattleBanzai.Components;

/// <summary>
/// A Battle Banzai random teleporter (<c>bb_rnd_tele</c> and friends). Stepping on it flashes it
/// and, half a second later, drops the walker on a random other teleporter in the room — inside a
/// match or outside one, because the furni is not gated on a round.
/// <para>
/// The <c>_exclude</c> variant never chains onto the teleporter it lands on. That is the documented
/// reading of the key's name and an assumption, not a captured behaviour: the Arcturus branch this
/// was verified against ships a single teleporter class.
/// </para>
/// </summary>
[RoomObjectLogic("battlebanzai_random_teleport")]
[RoomObjectLogic("battlebanzai_random_teleport_exclude")]
public sealed class BanzaiTeleportComponent(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : GameFurnitureLogic(stuffDataFactory, ctx), IRandomTeleportComponent
{
    public override GameId Game => BanzaiConstants.Game;

    public bool ChainsOnArrival { get; } =
        !ctx.Definition.LogicName.EndsWith("_exclude", StringComparison.Ordinal);
}
