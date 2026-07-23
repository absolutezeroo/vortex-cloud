using System;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Pure, in-memory firing cadence for a periodic wired trigger, anchored to the room clock rather than
/// to a per-box countdown. Each periodic furni has its own unit — <c>wf_trg_periodically</c> counts in
/// half-seconds (500ms), <c>wf_trg_period_short</c> in 50ms steps, <c>wf_trg_period_long</c> in
/// 5-second steps — so the schedule is parameterised by the milliseconds per configured unit and the
/// maximum unit count (both taken from the client slider).
/// <para>
/// Firing is a pure function of the room clock: the timeline is divided into fixed <see cref="DelayMs"/>
/// buckets measured from an anchor (0 = room-clock origin), and the box fires once as each new bucket
/// begins. This mirrors how Habbo describes the Repeat Effect trigger — it "synchronizes with the
/// internal room timer", so every periodic of the same interval is phase-locked and firing is immune to
/// the box being reloaded or moved (no local countdown to reset). The only thing that shifts the phase
/// is <see cref="Reset"/>, the server side of the Timer Reset effect, which re-anchors to "now" so the
/// box fires immediately and restarts its interval grid from there.
/// </para>
/// <para>
/// The last firing is remembered as an absolute timestamp, not as a bucket index, so that changing the
/// configured interval mid-run stays correct: both "now" and "last fired" are re-bucketed with the
/// current <see cref="DelayMs"/>, so a bucket recorded under the old interval can never freeze firing.
/// </para>
/// <para>Ephemeral runtime state — never persisted into <see cref="WiredData"/>; resets on box reload.</para>
/// </summary>
public sealed class WiredPeriodicSchedule(int msPerUnit, int maxUnits)
{
    private const long Never = long.MinValue;

    /// <summary>Room-clock origin the interval grid is measured from. <see cref="Reset"/> moves it to "now".</summary>
    private long _anchorMs;

    /// <summary>The room-clock time of the last firing; <see cref="Never"/> means "not fired yet".</summary>
    private long _lastFiredMs = Never;

    /// <summary>The player-configured slider value (int-param 0), clamped by <see cref="DelayMs"/>.</summary>
    public int DelayValue { get; set; } = 1;

    /// <summary>The interval between firings in milliseconds for this box.</summary>
    public int DelayMs => Math.Clamp(DelayValue, 1, maxUnits) * msPerUnit;

    /// <summary>Which fixed <see cref="DelayMs"/> bucket <paramref name="nowMs"/> falls into, from the anchor.</summary>
    private long BucketAt(long nowMs) => (nowMs - _anchorMs) / DelayMs;

    /// <summary>
    /// Whether a new interval bucket has begun since the last firing (so the box should fire now). A
    /// freshly loaded schedule (never fired) is immediately due, so a placed box fires on the next wired
    /// tick. Both timestamps are bucketed with the current <see cref="DelayMs"/>, so this stays correct
    /// across an interval change.
    /// </summary>
    public bool IsDue(long nowMs) => _lastFiredMs == Never || BucketAt(nowMs) > BucketAt(_lastFiredMs);

    /// <summary>Record that a firing happened at <paramref name="nowMs"/>.</summary>
    public void Advance(long nowMs) => _lastFiredMs = nowMs;

    /// <summary>
    /// Returns <c>true</c> if due at <paramref name="nowMs"/>, recording the firing time as it does so;
    /// <c>false</c> otherwise. Convenience for the room-tick poll.
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

    /// <summary>
    /// Server side of the Timer Reset effect: re-anchor the interval grid to <paramref name="nowMs"/> and
    /// forget the last firing so the box fires immediately on the next tick and counts its interval afresh
    /// from here.
    /// </summary>
    public void Reset(long nowMs)
    {
        _anchorMs = nowMs;
        _lastFiredMs = Never;
    }
}
