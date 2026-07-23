using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

[RoomObjectLogic("wf_trg_clock_counter")]
public class WiredTriggerCounterReachesTime(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.CLOCK_REACH_TIME;
    public override List<Type> SupportedEventTypes { get; } = [typeof(GameTimerTimeReachedEvent)];

    // Client ClockReachTime.ts: intParams = [seconds (0-119), minutes (0-99), subPulse (0-1)] = the
    // target time; the trigger fires when a running game timer's remaining time reaches it.
    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 119, 0),
            new WiredRangeParamRule(0, 99, 0),
            new WiredBoolParamRule(false),
        ];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (ctx.Event is not GameTimerTimeReachedEvent evt)
        {
            return Task.FromResult(false);
        }

        int seconds = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int minutes = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;

        return Task.FromResult(evt.RemainingSeconds == (minutes * 60) + seconds);
    }
}
