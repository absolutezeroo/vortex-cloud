using System;
using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// "The date range is active" — passes between the two moments typed into the box, which is how a
/// room runs an event that opens and closes by itself.
/// </summary>
/// <remarks>
/// The form is two free-text dates, and the box sends <b>however many of them parsed</b>: two ints,
/// one, or none at all. That is why the params are declared as a tail rather than a fixed count —
/// a fixed count would reject a half-filled box outright instead of letting it save.
/// <para>
/// Each is an absolute instant in Unix seconds, so no timezone is involved: the client resolves what
/// the player typed against their own clock before sending it.
/// </para>
/// <para>
/// A box with no start at all fails rather than passing. An unconfigured condition that let
/// everything through would be indistinguishable from one that was set up correctly.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_cnd_date_rng_active")]
public class WiredConditionDateRangeActive(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.DATE_RANGE_ACTIVE;

    public override List<IWiredParamRule> GetIntParamRules() => [];

    public override IWiredParamRule GetIntParamTailRule() => new WiredParamRule(0);

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        bool result = false;

        if (_wiredData.IntParams.Count > 0)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long start = _wiredData.IntParams[0];

            // One date means "from then on"; the form only sends the end when the start parsed too.
            long end = _wiredData.IntParams.Count > 1 ? _wiredData.IntParams[1] : long.MaxValue;

            result = now >= start && now <= end;
        }

        return IsNegative() ? !result : result;
    }
}
