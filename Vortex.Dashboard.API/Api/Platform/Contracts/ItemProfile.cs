using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One item and its complete history.
/// </summary>
/// <param name="Snapshot">What the item is right now, or null when the id names one that no longer
/// exists. The history is answered either way: an item that was deleted is exactly the one an
/// investigation is reading about.</param>
public sealed record ItemProfile(
    long ItemId,
    ItemSnapshot? Snapshot,
    int Page,
    int Limit,
    int Total,
    int Offset,
    int Count,
    IReadOnlyList<ItemEventRow> History
);
