using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Banzai;

/// <summary>
/// A Battle Banzai random teleporter (<c>bb_rnd_tele</c> and friends). Stepping on it flashes it
/// and, half a second later, drops the walker on a random other teleporter in the room. The
/// <c>_exclude</c> variant never chains onto the teleporter it lands on — the documented reading of
/// the key's name (an assumption; the Arcturus branch this was verified against has a single
/// teleporter class). Timing and chains are driven by <c>RoomBanzaiSystem</c>'s tick queues.
/// </summary>
[RoomObjectLogic("battlebanzai_random_teleport")]
[RoomObjectLogic("battlebanzai_random_teleport_exclude")]
public sealed class FurnitureBanzaiTeleportLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    /// <summary>True for <c>battlebanzai_random_teleport_exclude</c> — this one never chains.</summary>
    public bool IsExclude { get; } = ctx.Definition.LogicName.EndsWith("_exclude");

    public override bool CanWalk() => true;

    public override async Task OnWalkOnAsync(IRoomAvatarContext ctx, CancellationToken ct)
    {
        await base.OnWalkOnAsync(ctx, ct);

        if (ctx.RoomObject is IRoomPlayer player)
        {
            await _ctx.Banzai.OnTeleportWalkOnAsync(player.PlayerId, _ctx.RoomObject.ObjectId, ct);
        }
    }
}
