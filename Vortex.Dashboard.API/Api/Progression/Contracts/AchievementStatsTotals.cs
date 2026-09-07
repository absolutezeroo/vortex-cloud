namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The catalogue as a whole.
/// </summary>
/// <param name="MaxScoreAvailable">Every point the ladder could ever pay out, which is what a
/// player's score is measured against.</param>
public sealed record AchievementStatsTotals(
    int TotalAchievements,
    int TotalLevels,
    int TriggeredCount,
    int UntriggeredCount,
    int BadgesAwarded,
    int PlayersWithProgress,
    int MaxScoreAvailable
);
