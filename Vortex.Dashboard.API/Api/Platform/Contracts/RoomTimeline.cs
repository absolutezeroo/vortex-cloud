using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One room, and what happened in it -- entries, chat and item moves interleaved.
/// </summary>
/// <param name="Total">Every event in the window across the three journals, which is more than
/// <paramref name="Count"/>: the answer carries one page of the merged list.</param>
public sealed record RoomTimeline(
    RoomTimelineHeader Room,
    int Page,
    int Limit,
    int Offset,
    int Count,
    int Total,
    RoomTimelineTotals Totals,
    IReadOnlyList<RoomTimelineRow> Timeline
);
