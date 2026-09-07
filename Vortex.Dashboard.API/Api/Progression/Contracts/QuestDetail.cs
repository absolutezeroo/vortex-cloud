using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One quest with its full field set and its lifetime totals.</summary>
public sealed record QuestDetail(
    int Id,
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
    string CatalogPageName,
    string ImageVersion,
    string? ImageUrl,
    int SortOrder,
    bool Easy,
    bool Seasonal,
    int SeasonalSeconds,
    DateTime? EndsAt,
    bool Expired,
    int AcceptedCount,
    int CompletedCount
);
