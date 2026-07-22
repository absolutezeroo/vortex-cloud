using System;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Pure, in-memory firing cadence for a periodic wired trigger (<c>wf_trg_periodically</c> and
/// its short/long variants). It converts the configured delay into a room-clock interval and
/// tracks the next time the box is due to fire, re-arming exactly one interval at a time.
/// <para>
/// This is ephemeral runtime state — it is intentionally never persisted into
/// <see cref="WiredData"/> and resets whenever the wired box is (re)loaded, matching the
/// config/runtime boundary the wired subsystem enforces.
/// </para>
/// </summary>
public sealed class WiredPeriodicSchedule(WiredPeriodicTriggerType type)
{
    private long _nextFireMs;

    /// <summary>The player-configured delay knob (int-param 0), clamped by <see cref="DelayMs"/>.</summary>
    public int DelayValue { get; set; } = 1;

    /// <summary>The interval between firings, in milliseconds, for this box's period type.</summary>
    public int DelayMs =>
        type switch
        {
            WiredPeriodicTriggerType.Short => Math.Clamp(DelayValue, 1, 10) * 50,
            WiredPeriodicTriggerType.Long => Math.Clamp(DelayValue, 1, 120) * 5000,
            _ => 50,
        };

    /// <summary>
    /// Whether the configured interval has elapsed and the box should fire now. A freshly loaded
    /// schedule (never advanced) is immediately due, so a placed box fires on the next wired tick.
    /// </summary>
    public bool IsDue(long nowMs) => nowMs >= _nextFireMs;

    /// <summary>Arm the next firing one interval after <paramref name="nowMs"/>.</summary>
    public void Advance(long nowMs) => _nextFireMs = nowMs + DelayMs;

    /// <summary>
    /// Returns <c>true</c> if due at <paramref name="nowMs"/>, re-arming the next interval as it does
    /// so; <c>false</c> otherwise. Convenience for the room-tick poll.
    /// </summary>
    public bool TryConsumeDue(long nowMs)
    {
        if (!IsDue(nowMs))
        {
            return false;
        }

        Advance(nowMs);

        return true;
    }
}
