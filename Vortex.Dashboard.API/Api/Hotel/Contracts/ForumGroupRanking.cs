using System;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// A guild ranked by how much its forum is used.
/// </summary>
/// <param name="GroupName">Null when the guild has been deleted and only its threads remain: the
/// ranking is built from the threads, and the guild lookup can come back empty for one of them.
/// </param>
/// <param name="LastPostAt">Null for a forum whose threads have never been replied to.</param>
public sealed record ForumGroupRanking(
    int GroupId,
    string? GroupName,
    string? BadgeUrl,
    int Threads,
    int PostCount,
    DateTime? LastPostAt
);
