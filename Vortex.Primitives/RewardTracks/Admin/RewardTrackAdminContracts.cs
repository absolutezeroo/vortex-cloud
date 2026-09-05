using System;
using System.Collections.Generic;

namespace Vortex.Primitives.RewardTracks.Admin;

/// <summary>The outcome of one content write.</summary>
public sealed record RewardTrackAdminResult(bool Success, int? Id, string? ErrorCode)
{
    public static RewardTrackAdminResult Ok(int id) => new(true, id, null);

    public static RewardTrackAdminResult Fail(string errorCode) => new(false, null, errorCode);
}

/// <summary>Create/update spec for a track. Tasks and prizes are managed separately.</summary>
public sealed record RewardTrackSpec(
    string TrackId,
    string Theme,
    RewardTrackStatus Status,
    int SortOrder,
    DateTime? StartsAt,
    DateTime? ProgressEndsAt,
    DateTime? ClaimEndsAt,
    RewardTrackUnlockKind UnlockKind,
    string UnlockValue,
    RewardTrackCompletionPolicy CompletionPolicy,
    bool PremiumEnabled,
    int PremiumBoostPerMille,
    int PremiumInstantPoints,
    int PremiumCostCredits,
    int PremiumCostDiamonds,
    bool Hidden,
    string CampaignCode
);

/// <summary>Create/update spec for a task. <paramref name="Levels"/> replaces the whole stage ladder.</summary>
public sealed record RewardTrackTaskSpec(
    string TaskId,
    string ActionCode,
    string Parameter,
    TaskProgressMode Mode,
    bool Premium,
    int SortOrder,
    IReadOnlyList<RewardTrackTaskLevelSpec> Levels
);

/// <summary>One stage of a task.</summary>
public sealed record RewardTrackTaskLevelSpec(int RequiredCount, int PointsReward, bool Premium);

/// <summary>Create/update spec for a milestone. <paramref name="Rewards"/> replaces the whole bundle.</summary>
public sealed record RewardTrackPrizeSpec(
    string PrizeId,
    int RequiredPoints,
    bool Premium,
    int SortOrder,
    IReadOnlyList<RewardTrackRewardSpec> Rewards
);

/// <summary>One reward of a bundle.</summary>
public sealed record RewardTrackRewardSpec(
    RewardKind Kind,
    string RewardTypeId,
    int Amount,
    string ExtraParams,
    int SortOrder
);

/// <summary>Participation and conversion counts for one track, for the content list.</summary>
public sealed record RewardTrackStats(
    string TrackId,
    RewardTrackStatus Status,
    int TaskCount,
    int PrizeCount,
    int Participants,
    int Completions,
    int PremiumHolders,
    int PrizesClaimed
);

/// <summary>One player's standing on one track, for the progression inspector.</summary>
public sealed record PlayerRewardTrackAdminRow(
    string TrackId,
    int Points,
    bool PremiumUnlocked,
    DateTime? PremiumUnlockedAt,
    DateTime? CompletedAt,
    int TasksStarted,
    int PrizesClaimed
);

/// <summary>
/// A content problem. Reported rather than thrown so an operator sees every problem at once.
/// </summary>
/// <param name="TrackId">Which track it is in; empty for a problem spanning the catalog.</param>
public sealed record RewardTrackContentProblem(string TrackId, string Code, string Detail);

/// <summary>The validator's whole answer.</summary>
public sealed record RewardTrackContentReport(IReadOnlyList<RewardTrackContentProblem> Problems)
{
    public bool IsValid => Problems.Count == 0;
}
