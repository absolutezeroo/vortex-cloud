using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Rooms.Wired;

/// <summary>
/// How old a wired variable's value is, against the duration the box was configured with.
/// </summary>
/// <remarks>
/// The form pairs a number with a unit dropdown, and offers only two of the six comparisons —
/// "Lower than" (0) and "Higher than" (2) — so this is "younger than" and "older than" rather than
/// a full comparison.
/// </remarks>
public static class WiredVariableAge
{
    private const long Second = 1000L;

    private const long Minute = 60 * Second;

    private const long Hour = 60 * Minute;

    private const long Day = 24 * Hour;

    /// <summary>The configured duration in milliseconds, saturating rather than overflowing: the
    /// form accepts the whole int range against a unit as large as years, which is far beyond what
    /// a long of milliseconds holds.</summary>
    public static long ToMilliseconds(int duration, WiredTimeUnit unit)
    {
        long scale = unit switch
        {
            WiredTimeUnit.Milliseconds => 1,
            WiredTimeUnit.Seconds => Second,
            WiredTimeUnit.Minutes => Minute,
            WiredTimeUnit.Hours => Hour,
            WiredTimeUnit.Days => Day,
            WiredTimeUnit.Weeks => 7 * Day,
            WiredTimeUnit.Months => 30 * Day,
            WiredTimeUnit.Years => 365 * Day,
            _ => 1,
        };

        long limit = long.MaxValue / scale;

        return duration > limit ? long.MaxValue
            : duration < -limit ? long.MinValue
            : duration * scale;
    }

    /// <summary>
    /// Whether an age passes the box. Anything but the two comparisons the form offers fails
    /// closed, and a negative age — a value stamped in the future, which a clock change can produce
    /// — is read as zero rather than as "very young".
    /// </summary>
    public static bool Matches(long ageMs, WiredComparisonType comparison, long durationMs)
    {
        long age = ageMs < 0 ? 0 : ageMs;

        return comparison switch
        {
            WiredComparisonType.LessThan => age < durationMs,
            WiredComparisonType.GreaterThan => age > durationMs,
            _ => false,
        };
    }
}
