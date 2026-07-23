using System;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Pure, in-memory firing cadence for a periodic wired trigger. Each periodic furni has its own unit —
/// <c>wf_trg_periodically</c> counts in half-seconds (500ms), <c>wf_trg_period_short</c> in 50ms steps,
/// <c>wf_trg_period_long</c> in 5-second steps — so the schedule is parameterised by the milliseconds
/// per configured unit and the maximum unit count (both taken from the client slider). It tracks the
/// next fire time and re-arms exactly one interval at a time.
/// <para>Ephemeral runtime state — never persisted into <see cref="WiredData"/>; resets on box reload.</para>
/// </summary>
public sealed class WiredPeriodicSchedule(int msPerUnit, int maxUnits)
{
    private long _nextFireMs;

    /// <summary>The player-configured slider value (int-param 0), clamped by <see cref="DelayMs"/>.</summary>
    public int DelayValue { get; set; } = 1;

    /// <summary>The interval between firings in milliseconds for this box.</summary>
    public int DelayMs => Math.Clamp(DelayValue, 1, maxUnits) * msPerUnit;

    /// <summary>
    /// Whether the interval has elapsed and the box should fire now. A freshly loaded schedule (never
    /// advanced) is immediately due, so a placed box fires on the next wired tick.
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
