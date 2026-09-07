namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One category, counted.</summary>
public sealed record AchievementCategoryStats(
    string Category,
    int Achievements,
    int Levels,
    int BadgesAwarded
);
