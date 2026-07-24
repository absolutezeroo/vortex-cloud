using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Grains.Systems.Freeze;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Freeze;

/// <summary>
/// A Freeze ice block (the <c>es_box</c> furni). Intact it is a solid obstacle; a snowball blast destroys
/// it and, by chance, reveals one of the six power-ups, which a player collects by walking over the
/// broken block. Destruction, the power-up roll and collection are all driven by
/// <see cref="Systems.RoomFreezeSystem"/> — this logic only exposes state-based walkability and forwards
/// walk-on. Its state is ephemeral game display (reset to intact each round), so it is never persisted.
/// </summary>
[RoomObjectLogic("freeze_block")]
public sealed class FurnitureFreezeBlockLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    // Intact (state 0) is a solid obstacle; once destroyed the flat broken block is walkable so a player
    // can step over it to pick up whatever it revealed.
    public override bool CanWalk() => GetState() != FreezeConstants.BlockIntact;

    // A broken block is flat: it contributes no stack height, so the collector stands at floor level
    // rather than floating on top of the (now shattered) cube.
    public override Altitude GetStackHeight() =>
        GetState() == FreezeConstants.BlockIntact ? base.GetStackHeight() : Altitude.Zero;

    public override async Task OnWalkOnAsync(IRoomAvatarContext ctx, CancellationToken ct)
    {
        await base.OnWalkOnAsync(ctx, ct);

        if (ctx.RoomObject is IRoomPlayer player)
        {
            await _roomGrain.FreezeSystem.OnBlockWalkOnAsync(
                player.PlayerId,
                _ctx.RoomObject.X,
                _ctx.RoomObject.Y,
                ct
            );
        }
    }
}
