using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Snapshots.Mapping;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

[RoomObjectLogic("gate")]
public class FurnitureGateLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    private const int CLOSED_STATE = 0;
    private const int OPEN_STATE = 1;

    public override FurnitureUsageType GetUsagePolicy() => FurnitureUsageType.Controller;

    public override bool CanWalk()
    {
        int state = StuffData.GetState();

        if (state == OPEN_STATE)
        {
            return true;
        }

        return false;
    }

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        RoomTileSnapshot tile = await _ctx.GetTileSnapshotAsync(ct);

        if (tile.Flags.Has(RoomTileFlags.AvatarOccupied))
        {
            return;
        }

        await base.OnUseAsync(ctx, param, ct);
    }
}
