using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One page of a directory whose rows are codes rather than table rows.</summary>
public sealed record CodeDirectoryPage(
    int Count,
    int Total,
    int Offset,
    bool HasMore,
    IReadOnlyList<CodeDirectoryRow> Items
);
