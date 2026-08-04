using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

// The dump spells the same behaviour two ways: "pet_drink" on the waterbowl family, "petdrink" on
// the water_bowl1 one. Both are bowls; dropping either leaves those definitions inert.
[RoomObjectLogic("pet_drink")]
[RoomObjectLogic("petdrink")]
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
        // > 1, not > 0: a one-state bowl has no frame above 0, so the old test filled it to state 0
        // -- empty -- and stamped "0" into ExtraData. The room then skipped it forever, and feeding
        // read that 0 as "no servings left" and binned the bowl on first use. Leaving its data alone
        // lets the servings come from the pet_food row instead. That is the waterbowl_basic and
        // water_bowl1 families, fifteen bowls between them.
        if (_ctx.Definition.TotalStates > 1 && GetState() == 0)
        {
            int fullState = _ctx.Definition.TotalStates - 1;
            StuffData.SetState(fullState.ToString());
            _ctx.RoomObject.SetExtraData(fullState.ToString());
        }

        await base.OnPlaceAsync(ctx, ct);
    }
}
