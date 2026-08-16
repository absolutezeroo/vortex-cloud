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
/// "The time matches" — passes inside an hour, minute and second range, each of which can be
/// skipped independently.
/// </summary>
/// <remarks>
/// Nine int params, and their order is not the order the form draws: the three skip flags come
/// first, seconds before minutes before hours, and only then the bounds in the same reversed order
/// — <c>[useSeconds, useMinutes, useHours, secMin, secMax, minMin, minMax, hourMin, hourMax]</c>.
/// Reading them in screen order gives a box that filters hours by the seconds it was given.
/// <para>
/// The string param is the timezone; hotels offering a single one hide the dropdown and send
/// nothing, which reads as UTC.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_cnd_match_time")]
public class WiredConditionTimeMatches(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.TIME_MATCHES;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredBoolParamRule(false),
            new WiredBoolParamRule(false),
            new WiredBoolParamRule(false),
            new WiredRangeParamRule(0, 59, 0),
            new WiredRangeParamRule(0, 59, 0),
            new WiredRangeParamRule(0, 59, 0),
            new WiredRangeParamRule(0, 59, 0),
            new WiredRangeParamRule(0, 23, 0),
            new WiredRangeParamRule(0, 23, 0),
        ];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        bool result = false;

        if (_wiredData.IntParams.Count >= 9)
        {
            DateTime now = WiredTimeZone.Now(_wiredData.StringParam);

            result =
                WiredChronoFilter.RangeMatches(
                    _wiredData.GetIntParam<bool>(0),
                    now.Second,
                    _wiredData.GetIntParam<int>(3),
                    _wiredData.GetIntParam<int>(4)
                )
                && WiredChronoFilter.RangeMatches(
                    _wiredData.GetIntParam<bool>(1),
                    now.Minute,
                    _wiredData.GetIntParam<int>(5),
                    _wiredData.GetIntParam<int>(6)
                )
                && WiredChronoFilter.RangeMatches(
                    _wiredData.GetIntParam<bool>(2),
                    now.Hour,
                    _wiredData.GetIntParam<int>(7),
                    _wiredData.GetIntParam<int>(8)
                );
        }

        return IsNegative() ? !result : result;
    }
}
