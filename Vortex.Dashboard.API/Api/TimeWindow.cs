using System;
using System.Collections.Specialized;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// The since/until window every reporting read takes, and the calendar buckets a series over that
/// window is grouped into.
/// </summary>
/// <remarks>
/// <para>
/// Nine read subjects ask the same two questions — what period am I reporting on, and which bucket
/// does this row fall in — so this is one named thing rather than a copy per class. It stays
/// narrow on purpose: parsing and bucketing dates, no querying, no formatting of anything but a
/// bucket label.
/// </para>
/// <para>
/// The width limit is the point of <see cref="Resolve"/>, not a detail: a windowed read with no
/// ceiling is a table scan an operator can request by editing a URL.
/// </para>
/// </remarks>
internal static class TimeWindow
{
    /// <summary>The widest period any windowed read will serve.</summary>
    internal const int MAX_WINDOW_DAYS = 366;

    /// <summary>
    /// Resolves the <c>since</c>/<c>until</c> pair every windowed read takes, defaulting to the last
    /// <paramref name="defaultSpan" /> (30 days) and refusing anything wider than
    /// <see cref="MAX_WINDOW_DAYS" />. An inverted pair is swapped rather than refused — that one is
    /// unambiguous.
    /// </summary>
    internal static (DateTime Since, DateTime Until) Resolve(
        NameValueCollection query,
        DateTime nowUtc,
        TimeSpan? defaultSpan = null
    )
    {
        DateTime until = ParseDateTime(query["until"]) ?? nowUtc;
        DateTime since =
            ParseDateTime(query["since"]) ?? until - (defaultSpan ?? TimeSpan.FromDays(30));

        if (since > until)
        {
            (since, until) = (until, since);
        }

        if (until - since > TimeSpan.FromDays(MAX_WINDOW_DAYS))
        {
            throw new DashboardQueryException(
                "window_too_large",
                $"since/until must span at most {MAX_WINDOW_DAYS} days."
            );
        }

        return (since, until);
    }

    /// <summary>
    /// Null for an absent value, the parsed instant for a valid one, and a 400 for anything else.
    /// Returning null on garbage — the old behaviour — drops the filter, which widens the query
    /// instead of rejecting it.
    /// </summary>
    internal static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, out DateTimeOffset parsedOffset))
        {
            return parsedOffset.UtcDateTime;
        }

        if (DateTime.TryParse(value, out DateTime parsedDate))
        {
            return parsedDate;
        }

        throw new DashboardQueryException(
            "invalid_date",
            $"'{value}' is not a date the dashboard can parse."
        );
    }

    /// <summary>Day unless the caller asked for something coarser it is allowed to have.</summary>
    internal static string Granularity(string? value) =>
        (value ?? "").ToLowerInvariant() switch
        {
            "month" => "month",
            "year" => "year",
            _ => "day",
        };

    /// <summary>
    /// Calendar-aligned bucket for day/month/year, which handles variable-length months and years
    /// correctly instead of rounding to a fixed tick interval.
    /// </summary>
    internal static DateTime Bucket(DateTime value, string granularity) =>
        granularity switch
        {
            "year" => new DateTime(value.Year, 1, 1, 0, 0, 0, value.Kind),
            "month" => new DateTime(value.Year, value.Month, 1, 0, 0, 0, value.Kind),
            _ => value.Date,
        };

    internal static DateTime NextBucket(DateTime bucket, string granularity) =>
        granularity switch
        {
            "year" => bucket.AddYears(1),
            "month" => bucket.AddMonths(1),
            _ => bucket.AddDays(1),
        };

    internal static string Label(DateTime bucket, string granularity) =>
        granularity switch
        {
            "year" => bucket.ToString("yyyy"),
            "month" => bucket.ToString("yyyy-MM"),
            _ => bucket.ToString("yyyy-MM-dd"),
        };
}
