using System;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Reads the paging values out of a query string, with the clamping that keeps a typed URL from
/// asking for page -3 or ten thousand rows.
/// </summary>
/// <remarks>
/// <para>
/// Ten read subjects parse the same two parameters, so this is one named thing rather than a copy
/// per class. It is deliberately narrow — three parsers over <c>string?</c>, no query-string
/// walking, no defaults per page, nothing about HTTP — because a shared type that starts absorbing
/// "things reads need" is the catch-all the architecture rules name.
/// </para>
/// <para>
/// <see cref="DashboardApiService"/> keeps private copies of these until its last subject leaves;
/// switching its remaining topic files over would be a rename across ten files that no longer
/// exist once the migration finishes.
/// </para>
/// </remarks>
internal static class QueryValues
{
    /// <summary>An int, or the fallback when the value is absent or not a number.</summary>
    public static int Int(string? value, int fallback) =>
        int.TryParse(value, out int parsed) ? parsed : fallback;

    /// <summary>A page number, never below one.</summary>
    public static int Page(string? value) =>
        int.TryParse(value, out int page) ? Math.Max(1, page) : 1;

    /// <summary>A row count, clamped into what the endpoint is willing to serve.</summary>
    public static int Limit(string? value, int fallback, int max) =>
        int.TryParse(value, out int limit) ? Math.Clamp(limit, 1, max) : fallback;
}
