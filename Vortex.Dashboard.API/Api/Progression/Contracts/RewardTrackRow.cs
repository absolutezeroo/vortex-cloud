using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One reward track.
/// </summary>
/// <param name="FreePointCeiling">Every point a player can earn without paying. Sent because it is
/// what the content validator measures milestones against -- an operator can see why a prize is out
/// of reach without publishing to find out.</param>
/// <param name="PremiumPointCeiling">The same with premium unlocked.</param>
/// <param name="PrizesClaimed">Claims across every player, not players who claimed.</param>
public sealed record RewardTrackRow(
    int Id,
    string TrackId,
    string LocalizationKey,
    string Theme,
    string Status,
    int SortOrder,
    DateTime? StartsAt,
    DateTime? ProgressEndsAt,
    DateTime? ClaimEndsAt,
    string UnlockKind,
    string UnlockValue,
    string CompletionPolicy,
    bool PremiumEnabled,
    int PremiumBoostPerMille,
    int PremiumInstantPoints,
    int PremiumCostCredits,
    int PremiumCostDiamonds,
    int ContentVersion,
    bool Hidden,
    string CampaignCode,
    int FreePointCeiling,
    int PremiumPointCeiling,
    int Participants,
    int Completions,
    int PremiumHolders,
    int PrizesClaimed,
    IReadOnlyList<RewardTrackTaskRow> Tasks,
    IReadOnlyList<RewardTrackPrizeRow> Prizes
);
