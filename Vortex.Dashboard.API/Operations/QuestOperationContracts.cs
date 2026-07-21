using System;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the dashboard's quest admin operations, each carrying a mandatory audited
/// <c>Reason</c>. <c>RewardType</c> negative = Credits, otherwise the activity-point currency type
/// (0 = Duckets); <c>Seasonal</c> + <c>SeasonalSeconds</c>/<c>EndsAt</c> drive the configurable timer.
/// </summary>
public sealed record CreateQuestRequest(
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
    string CatalogPageName,
    string ImageVersion,
    int SortOrder,
    bool Easy,
    bool Seasonal,
    int SeasonalSeconds,
    DateTime? EndsAt,
    string Reason
);

public sealed record UpdateQuestRequest(
    int QuestId,
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
    string CatalogPageName,
    string ImageVersion,
    int SortOrder,
    bool Easy,
    bool Seasonal,
    int SeasonalSeconds,
    DateTime? EndsAt,
    string Reason
);

public sealed record DeleteQuestRequest(int QuestId, string Reason);
