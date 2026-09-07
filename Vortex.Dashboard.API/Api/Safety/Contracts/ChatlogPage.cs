using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// One page of room chat, always filtered.
/// </summary>
/// <remarks>
/// The window here carries no granularity: chatlogs are a list, not a series, so this is its own
/// two-field record rather than the shared <c>ReportWindow</c>. Adding a granularity would put a
/// field on the wire that means nothing.
/// </remarks>
public sealed record ChatlogPage(
    int Count,
    int Page,
    int Limit,
    int Total,
    int Offset,
    ChatlogWindow Window,
    ChatlogFilters Filters,
    IReadOnlyList<ChatlogEntry> Items
);
