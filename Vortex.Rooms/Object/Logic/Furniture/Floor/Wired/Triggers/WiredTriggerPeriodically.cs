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
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.TRIGGER_PERIODICALLY;
    public override List<Type> SupportedEventTypes { get; } = [typeof(PeriodicRoomEvent)];

    public virtual WiredPeriodicTriggerType PeriodicType => WiredPeriodicTriggerType.Short;

    // Ephemeral runtime cadence, (re)built from persisted config on load. Never serialized.
    private WiredPeriodicSchedule? _schedule;

    public override List<IWiredParamRule> GetIntParamRules() => [new WiredRangeParamRule(1, 10, 1)];

    /// <summary>Whether the configured interval has elapsed and the box is due to fire now.</summary>
    public bool IsDue(long nowMs) => _schedule?.IsDue(nowMs) ?? true;

    /// <summary>Arm the next firing one interval after <paramref name="nowMs"/>.</summary>
    public void ScheduleNextFire(long nowMs) => _schedule?.Advance(nowMs);

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        Task.FromResult(ctx.Event is PeriodicRoomEvent);

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        await base.FillInternalDataAsync(ct);

        int delayValue = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 1;

        _schedule = new WiredPeriodicSchedule(PeriodicType) { DelayValue = delayValue };
    }
}
