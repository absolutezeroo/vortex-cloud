namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One entry of the score leaderboard.
/// </summary>
/// <param name="Score">The sum of the completed rungs' points, not the achievements' full ladders.</param>
public sealed record AchievementScorePlayer(
    int PlayerId,
    string? PlayerName,
    int Score,
    int Badges
);
