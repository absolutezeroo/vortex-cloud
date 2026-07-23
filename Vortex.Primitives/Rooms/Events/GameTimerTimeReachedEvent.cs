namespace Vortex.Primitives.Rooms.Events;

/// <summary>
/// Raised each second a running game timer's displayed value changes, carrying the remaining seconds.
/// Drives the wired CLOCK_REACH_TIME trigger, which fires when the value matches its configured time.
/// </summary>
public sealed record GameTimerTimeReachedEvent : RoomEvent
{
    public required int RemainingSeconds { get; init; }
}
