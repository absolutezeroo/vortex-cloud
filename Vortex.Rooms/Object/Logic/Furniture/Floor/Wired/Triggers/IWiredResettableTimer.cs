namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

/// <summary>
/// A timed trigger whose firing can be restarted — the server side of the Timer Reset wired effect
/// (<c>wf_act_reset_timers</c>). Both kinds of timed trigger implement it, with different meanings:
/// a periodic repeater re-anchors its interval to "now" (so it fires on the next tick and counts afresh),
/// while an "at given time" one-shot re-arms so it can fire again at all — without a reset it fires
/// exactly once for the room's lifetime.
/// </summary>
public interface IWiredResettableTimer
{
    /// <summary>Restart this trigger's timing from <paramref name="nowMs"/> so it fires again.</summary>
    void ResetTimer(long nowMs);
}
