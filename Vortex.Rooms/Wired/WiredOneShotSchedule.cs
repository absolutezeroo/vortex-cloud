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
