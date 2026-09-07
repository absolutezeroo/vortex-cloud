namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// The queue at a glance.
/// </summary>
/// <param name="SanctionRate">Share of closed tickets that ended in a sanction, 0 when none closed.</param>
/// <param name="AvgResolutionMinutes">Mean time from picked to closed, 0 when nothing closed.</param>
public sealed record CfhTotals(
    int TotalTickets,
    int OpenCount,
    int PickedCount,
    int ClosedCount,
    int SanctionedCount,
    double SanctionRate,
    double AvgResolutionMinutes
);
