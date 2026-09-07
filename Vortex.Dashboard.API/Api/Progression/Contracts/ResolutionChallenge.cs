using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One challenge a player took on a statue.
/// </summary>
/// <param name="ReachedLevel">How far they actually got, so the row can read 2/3 rather than just
/// naming the target.</param>
/// <param name="State">completed, live or expired -- derived from the two dates, because a page
/// should not have to re-derive it and get the boundary wrong.</param>
public sealed record ResolutionChallenge(
    int Id,
    int PlayerId,
    string? PlayerName,
    int ItemId,
    int AchievementId,
    string? AchievementName,
    int TargetLevel,
    int ReachedLevel,
    DateTime StartedAt,
    DateTime EndsAt,
    DateTime? CompletedAt,
    string? BadgeCode,
    string? BadgeUrl,
    string State
);
