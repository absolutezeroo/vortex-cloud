using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One page of a picker directory.
/// </summary>
/// <param name="HasMore">Whether another page exists. The directories that answer in one go say
/// false and mean it; there is no second request to make.</param>
public sealed record DirectoryPage(
    int Count,
    int Total,
    int Offset,
    bool HasMore,
    IReadOnlyList<DirectoryRow> Items
);
