namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// A game's private sub-cadence inside the 50 ms room tick — "run this every N ms" without every game
/// hand-rolling a next-due field. The semantics deliberately match what Freeze's 1 Hz player tick
/// always did: after <see cref="Reset"/>, the first <see cref="Due"/> call arms the clock (returns
/// false and schedules now + period), then every call at or past the deadline fires and reschedules
/// from the current time — so a stalled room does not "catch up" with a burst of missed fires.
/// </summary>
public struct GameCadence(int periodMs)
{
    private readonly int _periodMs = periodMs;
    private long _nextDueMs;

    /// <summary>True when the period has elapsed; fires at most once per call and reschedules from
    /// <paramref name="nowMs"/>. The first call after construction or <see cref="Reset"/> only arms.</summary>
    public bool Due(long nowMs)
    {
        if (_nextDueMs == 0)
        {
            _nextDueMs = nowMs + _periodMs;

            return false;
        }

        if (nowMs < _nextDueMs)
        {
            return false;
        }

        _nextDueMs = nowMs + _periodMs;

        return true;
    }

    /// <summary>Re-arms the cadence: the next <see cref="Due"/> schedules afresh — call at round start.</summary>
    public void Reset() => _nextDueMs = 0;
}
