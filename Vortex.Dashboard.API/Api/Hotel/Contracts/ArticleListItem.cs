using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One article in the list.
/// </summary>
/// <param name="Scheduled">
/// Not a stored state: published with a date still ahead. The list says so because an editor
/// otherwise reads "Published" beside an article nobody can open yet.
/// </param>
/// <param name="Languages">Which translations exist, so a missing one is visible without opening it.</param>
public sealed record ArticleListItem(
    int Id,
    string Slug,
    string Category,
    string Status,
    bool Scheduled,
    DateTime? PublishAt,
    bool Pinned,
    string Author,
    string Title,
    IReadOnlyList<string> Languages
);
