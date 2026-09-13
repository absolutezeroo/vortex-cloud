namespace Vortex.Primitives.Events;

/// <summary>
/// A room decided it was being raided: arrivals crossed its threshold while protection was on.
/// </summary>
/// <param name="ArrivalsInWindow">How many distinct players arrived inside the detection window.</param>
/// <param name="Threshold">The threshold that was crossed, so a log line can be read years later
/// without knowing what the sensitivity meant at the time.</param>
/// <param name="GuardArmed">Whether a guard period was armed behind the incident.</param>
public sealed record RoomRaidDetectedEvent(
    int RoomId,
    int ArrivalsInWindow,
    int Threshold,
    bool GuardArmed
) : IEvent;
