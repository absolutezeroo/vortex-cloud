namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One rung of an achievement ladder.
/// </summary>
/// <param name="RewardType">The wallet the reward is paid into: negative means credits, anything
/// else is that activity-point type. <paramref name="RewardKind"/> is the same fact spelled out,
/// because the sign convention is not something a page should have to know.</param>
public sealed record AchievementLevel(
    int Level,
    string BadgeCode,
    string? BadgeUrl,
    int ProgressRequirement,
    int RewardAmount,
    int RewardType,
    string RewardKind,
    int ScorePoints
);
