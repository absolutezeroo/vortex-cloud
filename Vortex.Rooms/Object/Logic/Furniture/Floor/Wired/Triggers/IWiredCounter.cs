namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

/// <summary>
/// A furni whose countdown clock the wired <c>control_clock</c> / <c>adjust_clock</c> actions can
/// drive — i.e. the game timer furni (<see cref="FurnitureGameTimerLogic"/>). The furni owns and ticks
/// its own clock; these methods are just the control surface the actions call.
/// </summary>
public interface IWiredCounter
{
    /// <summary>control_clock Start — begin counting down from the selected duration.</summary>
    void StartClock();

    /// <summary>control_clock Stop / Pause — halt, keeping the current value.</summary>
    void HaltClock();

    /// <summary>control_clock Resume — continue counting down.</summary>
    void ResumeClock();

    /// <summary>control_clock Reset — halt and restore the selected duration.</summary>
    void ResetClock();

    /// <summary>adjust_clock Set value.</summary>
    void SetClockSeconds(int seconds);

    /// <summary>adjust_clock Increase / Decrease (pass a signed delta).</summary>
    void AddClockSeconds(int seconds);
}
