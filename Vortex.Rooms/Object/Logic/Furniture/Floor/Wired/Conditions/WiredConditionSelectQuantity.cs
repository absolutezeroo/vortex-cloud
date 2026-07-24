using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when the number of items in the stack's selection compares as configured against a
/// threshold (Habbo's "input source quantity"). Int params: [0] = source (1 counts the selected users,
/// 0 the selected furni), [1] = threshold quantity, [2] = comparison operator (0 Lower than, 1 Equals,
/// 2 Higher than — the same comparison radio the altitude condition uses). No negative variant.</summary>
[RoomObjectLogic("wf_cnd_slc_quantity")]
public class WiredConditionSelectQuantity(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.INPUT_SOURCE_QUANTITY;

    // [0] = source (0 furni / 1 users), [1] = threshold, [2] = operator (0 less / 1 eq / 2 more).
    // Rules must be declared or the client config update is rejected.
    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredBoolParamRule(false), new WiredParamRule(0), new WiredRangeParamRule(0, 2, 0)];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        bool countUsers = _wiredData.IntParams.Count > 0 && _wiredData.GetIntParam<bool>(0);
        int threshold = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;
        int op = _wiredData.IntParams.Count > 2 ? _wiredData.GetIntParam<int>(2) : 0;

        // Count the source the box was configured for; it previously always counted furni, ignoring
        // the user/furni toggle.
        int quantity = countUsers
            ? ctx.Selected.SelectedPlayerIds.Count
            : ctx.Selected.SelectedFurniIds.Count;

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
