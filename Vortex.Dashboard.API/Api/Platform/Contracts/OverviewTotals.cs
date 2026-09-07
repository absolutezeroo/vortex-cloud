using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// How big the three journals are.
/// </summary>
/// <param name="PerformanceSamplesSinceStart">In-memory since the process started, not a row count:
/// client performance telemetry is an OTel metric now and nothing writes it to a table.</param>
/// <param name="AsOf">When the counts were taken. They are full-table scans on tables that grow
/// without bound, so they are cached for half a minute -- this says how stale that is.</param>
public sealed record OverviewTotals(
    long Audit,
    long Ledger,
    long Items,
    long PerformanceSamplesSinceStart,
    DateTime AsOf
);
