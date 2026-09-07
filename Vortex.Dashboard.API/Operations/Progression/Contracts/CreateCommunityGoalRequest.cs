using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

/// <summary>
/// Request bodies for the quest-content admin operations. <c>Levels</c> and <c>Rewards</c> replace
/// what the goal or task had: neither holds player state, so rewriting them wholesale is safe and
/// spares the operator a write per row.
/// </summary>
public sealed record CreateCommunityGoalRequest(
    string Code,
    string CampaignCode,
    int ScorePerQuest,
    bool Enabled,
    DateTime? EndsAt,
    int SortOrder,
    IReadOnlyList<CommunityGoalLevelBody> Levels,
    string Reason
) : IReasonedRequest;
