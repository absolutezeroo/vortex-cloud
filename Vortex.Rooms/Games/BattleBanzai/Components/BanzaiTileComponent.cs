using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Games.BattleBanzai.Components;

/// <summary>
/// A Battle Banzai arena tile (<c>bb_patch1</c>, Arcturus key <c>battlebanzai_tile</c>). Walking on
/// it claims it for the walker's team — the whole claim/lock state machine is in the pure
/// <see cref="BanzaiBoard"/>, and this class holds no rules at all. Its state is the wire contract
/// the client's multistate visualization maps to colours.
/// </summary>
[RoomObjectLogic("battlebanzai_tile")]
public sealed class BanzaiTileComponent(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : GameFurnitureLogic(stuffDataFactory, ctx), IArenaTileComponent
{
    public override GameId Game => BanzaiConstants.Game;
}
