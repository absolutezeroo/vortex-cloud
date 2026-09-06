using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdateCommunityGoalRequest(
    int GoalId,
    string Code,
    string CampaignCode,
    int ScorePerQuest,
    bool Enabled,
    DateTime? EndsAt,
    int SortOrder,
    IReadOnlyList<CommunityGoalLevelBody> Levels,
    string Reason
) : IReasonedRequest;
