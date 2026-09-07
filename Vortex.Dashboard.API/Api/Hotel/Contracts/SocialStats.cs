using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>Who talks to whom: the friend graph, the messenger, and the guild forums.</summary>
public sealed record SocialStats(
    ReportWindow Window,
    SocialTotals Totals,
    IReadOnlyList<SocialTimelinePoint> Timeline,
    IReadOnlyList<SocialSenderCount> TopSenders,
    IReadOnlyList<SocialFriendedCount> TopFriended,
    SocialForums Forums
);
