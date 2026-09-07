using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Progression.Contracts;

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
) : IReasonedRequest;
