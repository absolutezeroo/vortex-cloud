using System;
using System.Collections.Generic;
using System.Globalization;
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

[RoomObjectLogic("wf_trg_clock_counter")]
public class WiredTriggerCounterReachesTime(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx), IWiredCounter
{
    public override int WiredCode => (int)WiredTriggerType.CLOCK_REACH_TIME;
    public override List<Type> SupportedEventTypes { get; } = [typeof(PeriodicRoomEvent)];

    // Ephemeral countdown, (re)built from persisted config on load. Never serialized.
    private readonly WiredCountdownClock _clock = new();

    // Client ClockReachTime.ts: intParams = [seconds (0-119), minutes (0-99), subPulse (0-1)]. This
    // is the countdown duration; the counter fires when that elapsed time is reached — i.e. when the
    // remaining time hits zero.
    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 119, 0),
            new WiredRangeParamRule(0, 99, 0),
            new WiredBoolParamRule(false),
        ];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        Task.FromResult(ctx.Event is PeriodicRoomEvent);

    public void StartClock() => _clock.Start();

    public void HaltClock() => _clock.Halt();

    public void ResumeClock() => _clock.Resume();

    public void ResetClock()
    {
        _clock.Reset();
        ApplyDisplay();
    }

    public void SetClockSeconds(int seconds)
    {
        _clock.SetSeconds(seconds);
        ApplyDisplay();
    }

    public void AddClockSeconds(int seconds)
    {
        _clock.AddSeconds(seconds);
        ApplyDisplay();
    }

    public bool AdvanceClock(long nowMs)
    {
        CountdownTick tick = _clock.Advance(nowMs);

        if (tick.DisplayChanged)
        {
            ApplyDisplay();
        }

        return tick.ReachedZero;
    }

    /// <summary>Mirror the remaining seconds onto the furni's state and broadcast it, without
    /// persisting (the countdown is ephemeral) and without raising a state-changed event.</summary>
    private void ApplyDisplay()
    {
        StuffData.SetState(_clock.RemainingSeconds.ToString(CultureInfo.InvariantCulture));

        _ = _ctx.RefreshStuffDataAsync();
    }

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        await base.FillInternalDataAsync(ct);

        int seconds = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int minutes = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;

        _clock.SetSeconds((minutes * 60) + seconds);

        // Seed the displayed state so a freshly loaded counter shows its configured time to clients.
        StuffData.SetState(_clock.RemainingSeconds.ToString(CultureInfo.InvariantCulture));
    }
}
