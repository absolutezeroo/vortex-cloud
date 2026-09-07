namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One rung, as the detail view shows it.
/// </summary>
/// <param name="Id">The rung's own row. The detail page deletes a rung by it, and without it the
/// delete posted an undefined id -- which is what typing this response found.</param>
public sealed record AchievementLadderRung(
    int Id,
    int Level,
    string BadgeCode,
    string? BadgeUrl,
    int ProgressRequirement,
    int RewardAmount,
    int RewardType,
    string RewardKind,
    int ScorePoints
);
