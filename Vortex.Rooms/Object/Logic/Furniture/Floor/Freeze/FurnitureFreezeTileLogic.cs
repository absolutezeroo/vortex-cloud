using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Freeze;

/// <summary>
/// A Freeze arena tile (the <c>es_tile</c> furni). The play area is simply the set of these placed in
/// the room — there is no bounding box. Players stand on them and throw a snowball by double-clicking
/// (the client sends a UseFurniture, handled here). Walkable, and its animation state (rise/blast/reset)
/// is ephemeral display driven by <see cref="Systems.RoomFreezeSystem"/>, so it is never persisted.
/// </summary>
[RoomObjectLogic("freeze_tile")]
public sealed class FurnitureFreezeTileLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    public override bool CanWalk() => true;

    public override Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct) =>
        _roomGrain.FreezeSystem.ThrowBallAsync(
            ctx.PlayerId,
            _ctx.RoomObject.X,
            _ctx.RoomObject.Y,
            ct
        );
}
