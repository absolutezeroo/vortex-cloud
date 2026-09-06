using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record CreateHabbiconCollectionRequest(
    string Code,
    int SortOrder,
    bool Enabled,
    bool Hidden,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    string CampaignCode,
    string Reason
) : IReasonedRequest;

public sealed record UpdateHabbiconCollectionRequest(
    int CollectionId,
    string Code,
    int SortOrder,
    bool Enabled,
    bool Hidden,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    string CampaignCode,
    string Reason
) : IReasonedRequest;

public sealed record DeleteHabbiconCollectionRequest(int CollectionId, string Reason)
    : IReasonedRequest;

public sealed record CreateHabbiconRequest(
    string Code,
    int CollectionId,
    int SortOrder,
    bool IsCollectionReward,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    bool Enabled,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    string Reason
) : IReasonedRequest;

public sealed record UpdateHabbiconRequest(
    int HabbiconId,
    string Code,
    int CollectionId,
    int SortOrder,
    bool IsCollectionReward,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    bool Enabled,
    DateTime? AvailableFrom,
    DateTime? AvailableUntil,
    string Reason
) : IReasonedRequest;

public sealed record DeleteHabbiconRequest(int HabbiconId, string Reason) : IReasonedRequest;

public sealed record GrantHabbiconRequest(int PlayerId, int HabbiconId, string Reason)
    : IReasonedRequest;

public sealed record RevokeHabbiconRequest(int PlayerId, int HabbiconId, string Reason)
    : IReasonedRequest;

// Reward tracks -----------------------------------------------------------------------------

public sealed record CreateRewardTrackRequest(
    string TrackId,
    string Theme,
    int SortOrder,
    DateTime? StartsAt,
    DateTime? ProgressEndsAt,
    DateTime? ClaimEndsAt,
    int UnlockKind,
    string UnlockValue,
    int CompletionPolicy,
    bool PremiumEnabled,
    int PremiumBoostPerMille,
    int PremiumInstantPoints,
    int PremiumCostCredits,
    int PremiumCostDiamonds,
    bool Hidden,
    string CampaignCode,
    string Reason
) : IReasonedRequest;

/// <summary>
/// Everything a create takes, plus the row id — and deliberately not the status. A track's
/// lifecycle moves through publish and archive, which validate; letting an update set it would be a
/// way past that.
/// </summary>
public sealed record UpdateRewardTrackRequest(
    int TrackRowId,
    string TrackId,
    string Theme,
    int SortOrder,
    DateTime? StartsAt,
    DateTime? ProgressEndsAt,
    DateTime? ClaimEndsAt,
    int UnlockKind,
    string UnlockValue,
    int CompletionPolicy,
    bool PremiumEnabled,
    int PremiumBoostPerMille,
    int PremiumInstantPoints,
    int PremiumCostCredits,
    int PremiumCostDiamonds,
    bool Hidden,
    string CampaignCode,
    string Reason
) : IReasonedRequest;

public sealed record CloneRewardTrackRequest(int TrackRowId, string NewTrackId, string Reason)
    : IReasonedRequest;

public sealed record RewardTrackRowRequest(int TrackRowId, string Reason) : IReasonedRequest;

public sealed record RewardTrackTaskLevelBody(int RequiredCount, int PointsReward, bool Premium);

/// <summary>One action in a task's sequence, with the tests a signal must pass to satisfy it.</summary>
public sealed record RewardTrackTaskStepBody(
    string ActionCode,
    IReadOnlyList<RewardTrackStepFilterBody>? Filters
);

/// <summary>
/// One test on a signal's facts. <c>Op</c> rather than <c>Operator</c>, which is a keyword in most
/// of the languages this JSON passes through.
/// </summary>
public sealed record RewardTrackStepFilterBody(string FactKey, int Op, string Value);

public sealed record UpsertRewardTrackTaskRequest(
    int TrackRowId,
    string TaskId,
    string ActionCode,
    string Parameter,
    int Mode,
    bool Premium,
    int SortOrder,
    IReadOnlyList<RewardTrackTaskLevelBody> Levels,
    IReadOnlyList<RewardTrackTaskStepBody>? Steps,
    string Reason
) : IReasonedRequest;

public sealed record DeleteRewardTrackTaskRequest(int TaskRowId, string Reason) : IReasonedRequest;

public sealed record RewardTrackRewardBody(
    int Kind,
    string RewardTypeId,
    int Amount,
    string ExtraParams,
    int SortOrder
);

public sealed record UpsertRewardTrackPrizeRequest(
    int TrackRowId,
    string PrizeId,
    int RequiredPoints,
    bool Premium,
    int SortOrder,
    IReadOnlyList<RewardTrackRewardBody> Rewards,
    string Reason
) : IReasonedRequest;

public sealed record DeleteRewardTrackPrizeRequest(int PrizeRowId, string Reason)
    : IReasonedRequest;

public sealed record ResetPlayerRewardTrackRequest(int PlayerId, string TrackId, string Reason)
    : IReasonedRequest;

public sealed record GrantRewardTrackPremiumRequest(int PlayerId, string TrackId, string Reason)
    : IReasonedRequest;
