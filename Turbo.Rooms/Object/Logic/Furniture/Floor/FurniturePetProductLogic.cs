using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor;

[RoomObjectLogic("furniture_pet_product")]
public class FurniturePetProductLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    public override async Task OnPlaceAsync(ActionContext ctx, CancellationToken ct)
    {
        if (_ctx.Definition.TotalStates > 0 && GetState() == 0)
        {
            int fullState = _ctx.Definition.TotalStates - 1;
            StuffData.SetState(fullState.ToString());
            _ctx.RoomObject.SetExtraData(fullState.ToString());
        }

        await base.OnPlaceAsync(ctx, ct);
    }
}
