using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One page of a file's entries.
/// </summary>
/// <param name="File">Which file these entries came from. The page keeps the previous rows on
/// screen while the next request travels, so without this it cannot tell one file's page from
/// another's -- and it renders each file with a different table.</param>
/// <param name="Error">Set when the file is unknown or unreadable, and then the rest is empty. The
/// page shows it instead of a table, which is the difference between "this file has no matches" and
/// "this file could not be opened".</param>
public sealed record GamedataEntryPage(
    string File,
    string? Error,
    DateTime? ModifiedUtc,
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<GamedataEntry> Entries
);
