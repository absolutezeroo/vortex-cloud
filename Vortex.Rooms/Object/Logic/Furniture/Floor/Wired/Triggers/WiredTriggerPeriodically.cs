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

[RoomObjectLogic("wf_trg_periodically")]
public class WiredTriggerPeriodically(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx), IWiredTimedTrigger
{
    public override int WiredCode => (int)WiredTriggerType.TRIGGER_PERIODICALLY;
    public override List<Type> SupportedEventTypes { get; } = [typeof(PeriodicRoomEvent)];

    // Client slider unit and max, per box type. wf_trg_periodically (TriggerPeriodically.ts) is a
    // 1..120 slider in half-second pulses → 500ms per unit. Overridden by the short/long variants.
    protected virtual int MsPerUnit => 500;
    protected virtual int MaxUnits => 120;

    // Ephemeral runtime cadence, (re)built from persisted config on load. Never serialized.
    private WiredPeriodicSchedule? _schedule;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredRangeParamRule(1, MaxUnits, 1)];

    public bool TryConsumeDue(long nowMs) => _schedule?.TryConsumeDue(nowMs) ?? false;

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        Task.FromResult(ctx.Event is PeriodicRoomEvent);

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        await base.FillInternalDataAsync(ct);

        int delayValue = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 1;

        _schedule = new WiredPeriodicSchedule(MsPerUnit, MaxUnits) { DelayValue = delayValue };
    }
}
