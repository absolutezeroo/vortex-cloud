using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One moving background object -- a falling snowflake, a drifting cloud.
/// </summary>
/// <remarks>
/// On the wire: <c>&lt;asset&gt;;&lt;motion&gt;;&lt;field&gt;;&lt;field&gt;…</c>, where the fields
/// after the motion are positional and mean different things per motion.
/// <para>
/// <paramref name="Index"/> is the slot number in the key, 1 to 20, and it is not decorative: the
/// <c>animated</c> motion restarts its frames when the object whose index it names resets its path,
/// so renumbering objects silently unlinks those animations.
/// </para>
/// </remarks>
public sealed record HotelViewObject(
    int Index,
    string Asset,
    string Motion,
    IReadOnlyList<string> Fields
);
