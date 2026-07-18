using System.Collections.Generic;
using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;
using Turbo.Rooms.Wired.Rules;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes while fewer than the configured number of seconds have elapsed since the room was
/// loaded (Habbo's "timer &lt;"). Int param [0] is the threshold in seconds. Pairs with
/// <see cref="WiredConditionTimerMoreThan"/>; no negative variant.</summary>
[RoomObjectLogic("wf_cnd_time_less_than")]
public class WiredConditionTimerLessThan(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.TIME_ELAPSED_LESS;

    // [0] = threshold in seconds. Rules must be declared or the client config update is rejected.
    public override List<IWiredParamRule> GetIntParamRules() => [new WiredParamRule(0)];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        int threshold = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        long elapsedSeconds = (_roomGrain.NowMs() - _roomGrain._state.EpochMs) / 1000;

        bool result = elapsedSeconds < threshold;

        return IsNegative() ? !result : result;
    }
}
