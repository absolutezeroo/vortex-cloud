namespace Vortex.Primitives.Rooms.RaidProtection;

/// <summary>
/// What the room decided about one arrival. Returned before the visitor is anywhere near the room's
/// avatar list, so that <see cref="Kick" /> and <see cref="Ban" /> mean "never let them in" rather
/// than "put them in and take them out again".
/// </summary>
public enum RaidEntryVerdict
{
    /// <summary>Nothing is happening, or this visitor is exempt.</summary>
    Allow = 0,

    /// <summary>Turned away. They may walk back in once the room quietens down.</summary>
    Kick = 1,

    /// <summary>Turned away and room-banned for the configured duration.</summary>
    Ban = 2,
}
