using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Freeze;

/// <summary>
/// A Freeze exit tile (the <c>es_exit</c> furni). Eliminated players are teleported onto one of these,
/// and a player who walks onto it leaves the game (forfeits). Always walkable.
/// </summary>
[RoomObjectLogic("freeze_exit")]
public sealed class FurnitureFreezeExitLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    public override bool CanWalk() => true;

    public override async Task OnWalkOnAsync(IRoomAvatarContext ctx, CancellationToken ct)
    {
        await base.OnWalkOnAsync(ctx, ct);

        if (ctx.RoomObject is IRoomPlayer player)
        {
            await _roomGrain.FreezeSystem.OnExitWalkOnAsync(player.PlayerId, ct);
        }
    }
}
