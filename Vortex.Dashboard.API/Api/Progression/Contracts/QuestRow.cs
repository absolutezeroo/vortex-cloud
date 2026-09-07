using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One quest.
/// </summary>
/// <param name="ImageUrl">image_version IS the asset filename, so a quest with an empty one shows
/// no picture in the client either -- which is worth seeing here.</param>
/// <param name="RewardKind">credits or activityPoints, spelled out: the sign convention on
/// RewardType is not something a page should have to know.</param>
/// <param name="Expired">Seasonal and past its date. It can still be enabled, which is the state
/// that looks configured and offers nothing.</param>
public sealed record QuestRow(
    int Id,
    string? ImageUrl,
    string CampaignCode,
    string ChainCode,
    string LocalizationCode,
    string QuestType,
    string TargetType,
    string TargetValue,
    bool Enabled,
    int TotalSteps,
    int RewardType,
    int RewardAmount,
    string RewardKind,
    int SortOrder,
    bool Easy,
    bool Seasonal,
    int SeasonalSeconds,
    DateTime? EndsAt,
    bool Expired,
    int AcceptedCount,
    int CompletedCount
);
