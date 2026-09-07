using System.Collections.Generic;
using Vortex.Dashboard.API.Api.Hotel.Contracts;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>How calls for help are arriving and how they end.</summary>
public sealed record CfhStats(
    ReportWindow Window,
    CfhTotals Totals,
    IReadOnlyList<CfhTimelinePoint> Timeline,
    IReadOnlyList<CfhCloseReasonCount> ByCloseReason,
    IReadOnlyList<CfhTopicCount> TopTopics,
    IReadOnlyList<CfhReportedPlayer> TopReportedPlayers
);
