namespace Vortex.Primitives.Rooms.Enums.Wired;

/// <summary>
/// The unit beside a duration on the "variable age" form, as the client's dropdown ids.
/// </summary>
public enum WiredTimeUnit
{
    Milliseconds = 0,
    Seconds = 1,
    Minutes = 2,
    Hours = 3,
    Days = 4,
    Weeks = 5,

    /// <summary>Counted as 30 days: the form offers no calendar to anchor a real month to.</summary>
    Months = 6,

    /// <summary>Counted as 365 days, for the same reason.</summary>
    Years = 7,
}
