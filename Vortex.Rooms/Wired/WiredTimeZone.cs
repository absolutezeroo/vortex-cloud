using System;

namespace Vortex.Rooms.Wired;

/// <summary>
/// The clock the calendar conditions read, from the timezone their form carries in the string
/// param.
/// </summary>
/// <remarks>
/// Wall clock, not the room clock: the room's is a monotonic counter that restarts with the room,
/// which cannot answer "is it Saturday". The dropdown is hidden entirely when a hotel offers one
/// timezone, so an empty string param is the ordinary case and means UTC here.
/// </remarks>
public static class WiredTimeZone
{
    /// <summary>The named zone, or UTC when the box carries no name or one this machine does not
    /// know — a hotel that moves to another host must not silently start matching different
    /// hours.</summary>
    public static TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (Exception ex)
            when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    /// <summary>The current time in that zone.</summary>
    public static DateTime Now(string? timeZoneId) =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Resolve(timeZoneId)).DateTime;
}
