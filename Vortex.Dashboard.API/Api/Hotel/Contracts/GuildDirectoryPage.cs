using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One page of the guild roster, as an operator needs to see it.</summary>
/// <remarks>
/// Not the same list as the picker's <c>DirectoryPage</c>: this one carries the counts an operator
/// is actually looking for. A guild with pending requests nobody has answered, or bans nobody has
/// reviewed, is the reason this page gets opened -- a bare id-and-name list would make them go into
/// every guild to find out.
/// </remarks>
public sealed record GuildDirectoryPage(
    int Page,
    int Limit,
    int Offset,
    int Total,
    int Count,
    IReadOnlyList<GuildDirectoryRow> Items
);
