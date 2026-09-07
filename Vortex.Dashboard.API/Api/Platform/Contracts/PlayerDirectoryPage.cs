using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One page of the player picker.
/// </summary>
/// <param name="HasMore">Whether another page exists. The online filter drops rows after the
/// projection, so the last page can come back short; a short page is the end of the list, which is
/// why this is not derived from <paramref name="Total"/> alone.</param>
/// <param name="Online">How many players are connected right now, across the hotel.</param>
public sealed record PlayerDirectoryPage(
    int Count,
    int Total,
    int Offset,
    bool HasMore,
    int Online,
    IReadOnlyList<PlayerDirectoryRow> Items
);
