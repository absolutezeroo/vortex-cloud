using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// Hotel-wide achievement health.
/// </summary>
/// <param name="Untouched">The definitions nobody has ever progressed -- the useful list, because
/// each is either a missing trigger or a requirement out of reach, and the row's own triggered flag
/// says which.</param>
/// <param name="BadgeImageTemplate">The url pattern, not a resolved url: the level editor previews
/// a badge code before its rung exists.</param>
public sealed record AchievementStats(
    AchievementStatsTotals Totals,
    IReadOnlyList<AchievementCategoryStats> ByCategory,
    IReadOnlyList<UntouchedAchievement> Untouched,
    IReadOnlyList<AchievementScorePlayer> TopPlayers,
    string? BadgeImageTemplate
);
