using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

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

public sealed record DeleteCommunityGoalRequest(int GoalId, string Reason) : IReasonedRequest;

/// <summary>
/// One rung. The level number is assigned server-side from the threshold order, because the client
/// pairs reward limits with levels by position.
/// </summary>
public sealed record CommunityGoalLevelBody(int ScoreThreshold, int RewardUserLimit);

public sealed record CreateDailyTaskRequest(
    string TaskCode,
    string QuestTypeCode,
    bool IsBonus,
    string ImageVersion,
    string CatalogName,
    int RequiredRepeats,
    bool Enabled,
    int SortOrder,
    IReadOnlyList<DailyTaskRewardBody> Rewards,
    string Reason
) : IReasonedRequest;

public sealed record UpdateDailyTaskRequest(
    int TaskId,
    string TaskCode,
    string QuestTypeCode,
    bool IsBonus,
    string ImageVersion,
    string CatalogName,
    int RequiredRepeats,
    bool Enabled,
    int SortOrder,
    IReadOnlyList<DailyTaskRewardBody> Rewards,
    string Reason
) : IReasonedRequest;

public sealed record DeleteDailyTaskRequest(int TaskId, string Reason) : IReasonedRequest;

/// <summary>
/// One reward. <c>RewardTypeId</c> is "credits" or an activity-point type number; anything else is
/// shown to the player but not granted, which the grain logs rather than swallowing.
/// </summary>
public sealed record DailyTaskRewardBody(
    short ProductItemTypeId,
    string RewardTypeId,
    string ExtraParams,
    int Amount
);
