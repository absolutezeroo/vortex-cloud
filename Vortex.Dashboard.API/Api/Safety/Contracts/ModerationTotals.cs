namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// The window's counters, with the paging that produced <c>Rows</c>.
/// </summary>
/// <param name="RetentionRate">Share of all room bans still in force, 0 when there are none.</param>
/// <param name="RenewalCount">Bans re-issued on a pair that already had one, which is the shape a
/// moderator repeating themselves makes.</param>
public sealed record ModerationTotals(
    int Total,
    int Limit,
    int Page,
    int Offset,
    int Success,
    int Denied,
    int Failed,
    double RetentionRate,
    int ActiveBans,
    int InactiveBans,
    int TotalBans,
    int RenewalCount,
    double AverageDurationSeconds
);
