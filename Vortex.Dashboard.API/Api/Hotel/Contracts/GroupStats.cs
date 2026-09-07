using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>How guilds and their forums are being used.</summary>
public sealed record GroupStats(
    ReportWindow Window,
    GroupTotals Totals,
    IReadOnlyList<GroupGrowthPoint> Growth,
    IReadOnlyList<GroupMemberRanking> TopGroupsByMembers,
    IReadOnlyList<GroupForumRanking> TopGroupsByForumActivity,
    IReadOnlyList<GroupActivityEvent> RecentActivity
);
