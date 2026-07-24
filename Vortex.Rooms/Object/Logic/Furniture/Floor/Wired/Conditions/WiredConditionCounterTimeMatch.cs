using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when one of the box's configured game timers shows a time matching the configured
/// target (Habbo's "clock time matches", ClockTimeMatches.ts): int params [seconds, minutes, subPulse,
/// comparison] where comparison is 0 = equal, 1 = less than, 2 = greater than. Compared against the
/// timer's displayed remaining seconds. The negative variant flips
/// <see cref="FurnitureWiredConditionLogic.IsNegative"/>.</summary>
[RoomObjectLogic("wf_cnd_counter_time_matches")]
public class WiredConditionCounterTimeMatch(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    // Client comparison radio (loc wiredfurni.params.comparison.*): 0 Lower than, 1 Equals,
    // 2 Higher than — the same order as WiredComparisonType.
    private const int ComparisonLess = 0;
    private const int ComparisonEqual = 1;
    private const int ComparisonGreater = 2;

    public override int WiredCode => (int)WiredConditionType.CLOCK_TIME_MATCHES;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 119, 0),
            new WiredRangeParamRule(0, 99, 0),
            new WiredBoolParamRule(false),
            new WiredRangeParamRule(0, 2, 0),
        ];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        int seconds = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int minutes = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;
        int comparison =
            _wiredData.IntParams.Count > 3 ? _wiredData.GetIntParam<int>(3) : ComparisonEqual;
        int target = (minutes * 60) + seconds;

        bool result = false;

        foreach (int furniId in GetStuffIds())
        {
            if (
                _roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                && item.Logic is FurnitureGameTimerLogic timer
            )
            {
                int value = timer.RemainingSeconds;

                bool match = comparison switch
                {
                    ComparisonLess => value < target,
                    ComparisonGreater => value > target,
                    _ => value == target,
                };

                if (match)
                {
                    result = true;
                    break;
                }
            }
        }

        return IsNegative() ? !result : result;
    }
}
