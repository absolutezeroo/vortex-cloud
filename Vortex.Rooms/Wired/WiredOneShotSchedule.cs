namespace Vortex.Rooms.Wired;

/// <summary>
/// Pure, in-memory one-shot cadence for the wired "trigger once" trigger (<c>wf_trg_at_given_time</c>).
/// It arms itself the first time it is polled and then fires exactly once, one delay later. All state
/// is ephemeral runtime state — never persisted into <see cref="WiredData"/> — so a room reload or a
/// reconfigure re-arms it.
/// </summary>
public sealed class WiredOneShotSchedule(int delayMs)
{
    private long _fireAtMs = -1;
    private bool _fired;

    /// <summary>The configured one-shot delay, used to detect a reconfigure (which re-arms the box).</summary>
    public int DelayMs => delayMs;

    /// <summary>
    /// Re-arm the one-shot so it can fire again: the next poll restarts the delay from that moment. This
    /// is what the Timer Reset effect (<c>wf_act_reset_timers</c>) does to an "at given time" trigger —
    /// without it the box fires exactly once for the lifetime of the room.
    /// </summary>
    public void Reset()
    {
        _fireAtMs = -1;
        _fired = false;
    }

    /// <summary>
    /// Returns <c>true</c> exactly once — on the first poll at or after the armed fire time — and
    /// <c>false</c> every other time. The first poll only arms the timer (fire time = that poll's
    /// <paramref name="nowMs"/> + delay), so the delay is measured from when the box goes live.
    /// </summary>
    public bool TryConsumeDue(long nowMs)
    {
        if (_fired)
        {
            return false;
        }

        if (_fireAtMs < 0)
        {
            _fireAtMs = nowMs + delayMs;

            return false;
        }

        if (nowMs < _fireAtMs)
        {
            return false;
        }

        _fired = true;

        return true;
    }
}
