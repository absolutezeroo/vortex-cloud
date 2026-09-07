using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One community goal.
/// </summary>
/// <param name="IsActive">The one goal players are actually contributing to. Decided by the same
/// rule the grain applies -- first enabled and unexpired -- so the page cannot disagree with what
/// players see.</param>
/// <param name="Expired">Past its date, whether or not it is still enabled.</param>
/// <param name="ReachedLevel">How many rungs the total has passed.</param>
public sealed record CommunityGoalRow(
    int Id,
    string Code,
    string CampaignCode,
    int ScorePerQuest,
    bool Enabled,
    DateTime? EndsAt,
    int SortOrder,
    bool Expired,
    bool IsActive,
    int TotalScore,
    int Contributors,
    int ReachedLevel,
    IReadOnlyList<CommunityGoalLevelRow> Levels
);
