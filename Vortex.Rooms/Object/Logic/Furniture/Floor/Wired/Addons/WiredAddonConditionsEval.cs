using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// The condition-evaluation addon: it decides how many of the pile's conditions must pass, replacing
/// the default "all of them". The client (ConditionEvaluation.ts, labelled "Conditions that need to
/// match:") offers seven choices — All, At least one, Not all, None, and three counting comparisons.
/// </summary>
[RoomObjectLogic("wf_xtra_one_condition")]
public class WiredAddonConditionsEval(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    // Wire encoding, from ConditionEvaluation.readIntParamsFromForm(): a set mode is sent as its own
    // id 0..3; picking one of the three counting comparisons instead sends mode = -1 and puts the
    // comparison in the next two slots.
    private const int CompareMode = -1;

    public override int WiredCode => (int)WiredAddonType.CONDITION_EVALUATION;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(CompareMode, 3, 0), // eval mode, or -1 for a count comparison
            new WiredRangeParamRule(0, 2, 0), // 0 less than, 1 exactly, 2 more than
            new WiredRangeParamRule(0, 1000, 0), // the N being compared against
        ];

    public override Task<bool> MutatePolicyAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        int mode = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int compareType = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;
        int compareValue = _wiredData.IntParams.Count > 2 ? _wiredData.GetIntParam<int>(2) : 0;

        ctx.Policy.ConditionMode =
            mode == CompareMode
                ? (WiredConditionModeType)(
                    (int)WiredConditionModeType.CountLessThan + Math.Clamp(compareType, 0, 2)
                )
                : (WiredConditionModeType)Math.Clamp(mode, 0, 3);

        ctx.Policy.ConditionCompareValue = compareValue;

        return Task.FromResult(true);
    }
}
