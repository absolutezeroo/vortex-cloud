using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Banzai;

/// <summary>
/// A Battle Banzai arena tile (<c>bb_patch1</c>, Arcturus key <c>battlebanzai_tile</c>). Walking on
/// it claims it for the walker's team — the whole claim/lock state machine lives in the pure
/// <c>BanzaiBoard</c>; this logic only reports the step. The tile's state is the wire contract the
/// client's multistate visualization maps to colours, live per round and never persisted.
/// </summary>
[RoomObjectLogic("battlebanzai_tile")]
public sealed class FurnitureBanzaiTileLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    public override bool CanWalk() => true;

    public override async Task OnWalkOnAsync(IRoomAvatarContext ctx, CancellationToken ct)
    {
        await base.OnWalkOnAsync(ctx, ct);

        if (ctx.RoomObject is IRoomPlayer player && _ctx.RoomObject is IRoomFloorItem floor)
        {
            await _ctx.Banzai.OnTileWalkOnAsync(
                player.PlayerId,
                _ctx.Map.ToIdx(floor.X, floor.Y),
                ct
            );
        }
    }
}
