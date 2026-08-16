using System;
using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// "The date matches" — passes on the ticked weekdays and months, inside a day-of-month and a year
/// range.
/// </summary>
/// <remarks>
/// Eight int params: <c>[useDay, useYear, weekdayMask, dayMin, dayMax, monthMask, yearMin,
/// yearMax]</c>. The two masks are checkbox groups whose bits start at zero over labels that start
/// at one, so <b>bit 0 is Monday</b> — a hotel numbering its week from Sunday would shift every
/// box by a day — and bit 0 is January. An empty mask is "any", not "none".
/// <para>
/// The string param is the timezone, as on the time condition.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_cnd_match_date")]
public class WiredConditionDateMatches(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    private const int AllWeekdays = 0b111_1111;

    private const int AllMonths = 0b1111_1111_1111;

    public override int WiredCode => (int)WiredConditionType.DATE_MATCHES;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredBoolParamRule(false),
            new WiredBoolParamRule(false),
            new WiredRangeParamRule(0, AllWeekdays, 0),
            new WiredRangeParamRule(1, 31, 1),
            new WiredRangeParamRule(1, 31, 1),
            new WiredRangeParamRule(0, AllMonths, 0),
            new WiredRangeParamRule(0, 9999, 0),
            new WiredRangeParamRule(0, 9999, 0),
        ];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        bool result = false;

        if (_wiredData.IntParams.Count >= 8)
        {
            DateTime now = WiredTimeZone.Now(_wiredData.StringParam);

            // DayOfWeek counts from Sunday = 0; the client's checkboxes count from Monday = bit 0.
            int weekday = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;

            result =
                WiredChronoFilter.MaskMatches(_wiredData.GetIntParam<int>(2), weekday)
                && WiredChronoFilter.MaskMatches(_wiredData.GetIntParam<int>(5), now.Month)
                && WiredChronoFilter.RangeMatches(
                    _wiredData.GetIntParam<bool>(0),
                    now.Day,
                    _wiredData.GetIntParam<int>(3),
                    _wiredData.GetIntParam<int>(4)
                )
                && WiredChronoFilter.RangeMatches(
                    _wiredData.GetIntParam<bool>(1),
                    now.Year,
                    _wiredData.GetIntParam<int>(6),
                    _wiredData.GetIntParam<int>(7)
                );
        }

        return IsNegative() ? !result : result;
    }
}
