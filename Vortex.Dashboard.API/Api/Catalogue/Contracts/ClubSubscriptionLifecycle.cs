using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>What happened to subscriptions during the window, from the audit trail.</summary>
public sealed record ClubSubscriptionLifecycle(
    ClubLifecycleTotals Totals,
    IReadOnlyList<ClubMonthsBreakdown> ByMonths,
    IReadOnlyList<ClubSubscriptionEventRow> RecentEvents,
    IReadOnlyList<ClubLifecyclePoint> Timeline
);
