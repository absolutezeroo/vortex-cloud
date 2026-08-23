using System;

namespace Vortex.Dashboard.API.Api;

/// <summary>
///     A read request the operator got wrong: an unparsable date, a window wider than the reads are
///     willing to scan. Thrown from the query helpers and turned into a 400 with <see cref="Error" />
///     as the body's <c>error</c> code by the dashboard's exception middleware. Silently ignoring the
///     bad value instead — which is what the helpers used to do — does not narrow the query, it
///     *widens* it, so the operator gets a slower, wrong answer with no hint that a filter was dropped.
/// </summary>
internal sealed class DashboardQueryException(string error, string message) : Exception(message)
{
    public string Error { get; } = error;
}
