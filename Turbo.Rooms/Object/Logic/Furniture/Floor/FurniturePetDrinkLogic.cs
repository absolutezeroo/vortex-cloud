using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor;

[RoomObjectLogic("pet_drink")]
public class FurniturePetDrinkLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
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
