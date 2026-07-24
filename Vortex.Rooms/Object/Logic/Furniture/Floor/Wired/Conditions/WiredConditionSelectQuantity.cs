using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when the number of furni carried in the stack's selection compares as configured
/// against a threshold (Habbo's "input source quantity"). Int params: [1] = threshold quantity,
/// [2] = comparison operator (0 = equal, 1 = less than, 2 = greater than — the same comparison radio
/// the altitude condition uses). No negative variant.</summary>
[RoomObjectLogic("wf_cnd_slc_quantity")]
public class WiredConditionSelectQuantity(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.INPUT_SOURCE_QUANTITY;

    // [0] = flag (unused here), [1] = threshold, [2] = operator (0 eq / 1 less / 2 more). Rules must be
    // declared or the client config update is rejected.
    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredBoolParamRule(false), new WiredParamRule(0), new WiredRangeParamRule(0, 2, 0)];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        int threshold = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;
        int op = _wiredData.IntParams.Count > 2 ? _wiredData.GetIntParam<int>(2) : 0;
        int quantity = ctx.Selected.SelectedFurniIds.Count;

        bool result = op switch
        {
            0 => quantity < threshold, // Lower than
            1 => quantity == threshold, // Equals
            2 => quantity > threshold, // Higher than
            _ => false,
        };

        return IsNegative() ? !result : result;
    }
}
