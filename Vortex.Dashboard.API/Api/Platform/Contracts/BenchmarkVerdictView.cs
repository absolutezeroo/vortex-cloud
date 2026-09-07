using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// How the run should be read, and why.
/// </summary>
/// <remarks>
/// A page full of milliseconds says nothing unless you already know what good looks like, which is
/// exactly what somebody running their first load test does not. The judgement is made once, on the
/// server, in terms of what a player would have felt -- and the same one the report file stores, so
/// the page and the file always agree.
/// </remarks>
/// <param name="Grade">Good, Watch or Bad, as the enum's own name.</param>
/// <param name="TickBudgetPercent">The slowest tick as a share of the 50 ms a tick is allowed. Over
/// 100 is an avatar that does not move when the player says so.</param>
public sealed record BenchmarkVerdictView(
    string Grade,
    string Headline,
    IReadOnlyList<string> Findings,
    double MedianRttMs,
    int Stalls,
    double TickBudgetPercent
);
