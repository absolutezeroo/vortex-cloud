namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

/// <summary>
/// A wired game counter furni (<c>wf_trg_clock_counter</c>): a countdown clock the wired system ticks
/// each frame and that the control_clock / adjust_clock actions drive. The furni both displays its
/// remaining time and fires CLOCK_REACH_TIME when the countdown reaches zero.
/// </summary>
public interface IWiredCounter
{
    /// <summary>control_clock Start — begin counting down from the current value.</summary>
    void StartClock();

    /// <summary>control_clock Stop / Pause — halt, keeping the current value.</summary>
    void HaltClock();

    /// <summary>control_clock Resume — continue counting down.</summary>
    void ResumeClock();

    /// <summary>control_clock Reset — halt and restore the configured value.</summary>
    void ResetClock();

    /// <summary>adjust_clock Set value.</summary>
    void SetClockSeconds(int seconds);

    /// <summary>adjust_clock Increase / Decrease (pass a signed delta).</summary>
    void AddClockSeconds(int seconds);

    /// <summary>
    /// Advance the countdown to <paramref name="nowMs"/>, refreshing the furni's displayed value when
    /// the whole-second changes. Returns <c>true</c> the moment it reaches zero, so the wired system
    /// fires the counter's CLOCK_REACH_TIME trigger exactly once.
    /// </summary>
    bool AdvanceClock(long nowMs);
}
