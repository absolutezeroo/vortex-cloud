using System;

namespace Vortex.Rooms.Wired;

/// <summary>
/// The two shapes the calendar conditions filter time with: a range that can be skipped, and a
/// checkbox mask over named values (weekdays, months).
/// </summary>
/// <remarks>
/// Both have a "no restriction" state that is easy to get backwards. A range carries its own skip
/// flag, so an unused field must not be compared at all; a mask has no skip option, so an empty one
/// is the client's way of saying "any" — reading it as "none" would make a freshly placed box
/// impossible to satisfy, and the client would happily let a player save it.
/// </remarks>
public static class WiredChronoFilter
{
    /// <summary>Whether a field passes its range, inclusive at both ends. A field whose filter is
    /// off passes whatever its value.</summary>
    public static bool RangeMatches(bool use, int value, int min, int max) =>
        !use || (value >= Math.Min(min, max) && value <= Math.Max(min, max));

    /// <summary>
    /// Whether a one-based value (weekday 1-7, month 1-12) is ticked in the mask. The client builds
    /// its checkboxes from labels numbered from one but indexes their bits from zero, so weekday 1 —
    /// Monday, not Sunday — is bit 0.
    /// </summary>
    public static bool MaskMatches(int mask, int oneBasedValue)
    {
        if (mask == 0)
        {
            return true;
        }

        if (oneBasedValue is < 1 or > 32)
        {
            return false;
        }

        return (mask & (1 << (oneBasedValue - 1))) != 0;
    }
}
