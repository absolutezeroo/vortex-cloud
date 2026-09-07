using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One page of a file's entries.
/// </summary>
/// <param name="Error">Set when the file is unknown or unreadable, and then the rest is empty. The
/// page shows it instead of a table, which is the difference between "this file has no matches" and
/// "this file could not be opened".</param>
public sealed record GamedataEntryPage(
    string? Error,
    DateTime? ModifiedUtc,
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<GamedataEntry> Entries
);
