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
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

[RoomObjectLogic("wf_trg_at_given_time")]
public class WiredTriggerAtTime(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
)
    : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx),
        IWiredTimedTrigger,
        IWiredResettableTimer
{
    // Client (TriggerOnce.ts): the slider is in "pulses", where pulses / 2 = seconds → 500ms/pulse.
    private const int MsPerPulse = 500;

    public override int WiredCode => (int)WiredTriggerType.TRIGGER_ONCE;
    public override List<Type> SupportedEventTypes { get; } = [typeof(PeriodicRoomEvent)];

    // Ephemeral runtime cadence, (re)built from persisted config on load. Never serialized.
    private WiredOneShotSchedule? _schedule;

    // intParams[0] = delay in pulses (1..1200), matching the client slider.
    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredRangeParamRule(1, 1200, 1)];

    public bool TryConsumeDue(long nowMs) => _schedule?.TryConsumeDue(nowMs) ?? false;

    // Server side of the Timer Reset effect: re-arm so this one-shot can fire again. Without a reset an
    // "at given time" box fires exactly once for the room's lifetime, which is Habbo's behaviour.
    public void ResetTimer(long nowMs) => _schedule?.Reset();

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        Task.FromResult(ctx.Event is PeriodicRoomEvent);

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        await base.FillInternalDataAsync(ct);

        int pulses = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 1;
        int delayMs = pulses * MsPerPulse;

        // Preserve the schedule across reloads. LoadWiredAsync (and thus this method) runs every time the
        // pile is resolved live — on every fire and every reindex — and recreating the schedule would
        // clear its "already fired" marker, so this one-shot would re-arm and fire over and over instead
        // of exactly once. Rebuild only when the configured time actually changed: that is the
        // reconfigure case, which legitimately re-arms the box (Arcturus does the same in saveData).
        if (_schedule is null || _schedule.DelayMs != delayMs)
        {
            _schedule = new WiredOneShotSchedule(delayMs);
        }
    }
}
