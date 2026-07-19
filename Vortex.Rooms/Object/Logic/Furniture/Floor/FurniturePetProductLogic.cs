using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

[RoomObjectLogic("pet_food")]
public class FurniturePetProductLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    public override async Task OnAttachAsync(CancellationToken ct)
    {
        // Sync plain-string ExtraData → StuffData so clients see the correct bowl state
        string raw = _ctx.RoomObject.ExtraData.GetJsonString();
        if (int.TryParse(raw, out int state) && state > 0)
        {
            StuffData.SetState(raw);
        }

        await base.OnAttachAsync(ct);
    }

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
