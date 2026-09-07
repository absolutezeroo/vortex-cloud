using System;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One thread in the recent-activity list.
/// </summary>
/// <param name="GroupName">Null when the guild has been deleted, as in <see cref="ForumGroupRanking"/>.</param>
/// <param name="LastPostAt">Null for a thread nobody has replied to.</param>
public sealed record ForumThreadSummary(
    int Id,
    int GroupId,
    string? GroupName,
    string? BadgeUrl,
    string Subject,
    string State,
    bool IsPinned,
    int PostCount,
    DateTime? LastPostAt,
    DateTime CreatedAt,
    int AuthorId,
    string? AuthorName
);
