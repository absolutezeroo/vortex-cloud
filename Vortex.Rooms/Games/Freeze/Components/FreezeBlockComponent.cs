using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Games;

namespace Vortex.Rooms.Games.Freeze.Components;

/// <summary>
/// A Freeze ice block (<c>es_box</c>). Intact it is a solid obstacle; a snowball blast destroys it
/// and, by chance, reveals a power-up that a player collects by walking over the broken block. The
/// destruction, the roll and the collection are all rules, and all of them live in
/// <see cref="FreezeGame"/> — this class contributes walkability and stack height, which are
/// physical properties of the furni rather than rules.
/// </summary>
[RoomObjectLogic("freeze_block")]
public sealed class FreezeBlockComponent(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : GameFurnitureLogic(stuffDataFactory, ctx), IDestructibleComponent
{
    public override GameId Game => FreezeConstants.Game;

    public int IntactState => FreezeConstants.BlockIntact;

    // Intact (state 0) is a solid obstacle; once destroyed the flat broken block is walkable so a
    // player can step over it to pick up whatever it revealed.
    public override bool CanWalk() => GetState() != FreezeConstants.BlockIntact;

    // A broken block is flat: it contributes no stack height, so the collector stands at floor level
    // rather than floating on top of the shattered cube.
    public override Altitude GetStackHeight() =>
        GetState() == FreezeConstants.BlockIntact ? base.GetStackHeight() : Altitude.Zero;
}
