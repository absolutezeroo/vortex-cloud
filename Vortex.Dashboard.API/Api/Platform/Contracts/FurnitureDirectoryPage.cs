using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One page of the furniture-definition picker.</summary>
public sealed record FurnitureDirectoryPage(
    int Count,
    int Total,
    int Offset,
    bool HasMore,
    IReadOnlyList<FurnitureDirectoryRow> Items
);
