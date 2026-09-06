using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record AchievementLevelRequest(
    int AchievementId,
    int Level,
    string BadgeCode,
    int ProgressRequirement,
    int RewardAmount,
    int RewardType,
    int ScorePoints,
    string Reason
) : IReasonedRequest;
