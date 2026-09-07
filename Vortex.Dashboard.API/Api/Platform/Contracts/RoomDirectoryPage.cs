using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One page of the room picker.</summary>
public sealed record RoomDirectoryPage(
    int Count,
    int Total,
    int Offset,
    bool HasMore,
    IReadOnlyList<RoomDirectoryRow> Items
);
