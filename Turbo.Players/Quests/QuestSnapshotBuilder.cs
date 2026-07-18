using System;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.Players.Quests;

/// <summary>
/// Pure mapping from a cached quest definition + the player's stored progress (plus campaign-level
/// aggregates the grain computes) to the wire-ready <see cref="QuestSnapshot"/>. Kept free of
/// Orleans/DB so the mapping can be unit-tested.
/// </summary>
public static class QuestSnapshotBuilder
{
    public static QuestSnapshot Build(
        QuestDefinitionSnapshot definition,
        int completedSteps,
        bool accepted,
        bool completed,
        int completedQuestsInCampaign,
        int questCountInCampaign
    )
    {
        int steps = completed
            ? definition.TotalSteps
            : Math.Clamp(completedSteps, 0, definition.TotalSteps);

        return new QuestSnapshot
        {
            CampaignCode = definition.CampaignCode,
            CompletedQuestsInCampaign = completedQuestsInCampaign,
            QuestCountInCampaign = questCountInCampaign,
            ActivityPointType = definition.RewardType,
            Id = definition.Id,
            Accepted = accepted,
            QuestType = definition.QuestType,
            ImageVersion = definition.ImageVersion,
            RewardCurrencyAmount = definition.RewardAmount,
            LocalizationCode = definition.LocalizationCode,
            CompletedSteps = steps,
            TotalSteps = definition.TotalSteps,
            SortOrder = definition.SortOrder,
            CatalogPageName = definition.CatalogPageName,
            ChainCode = definition.ChainCode,
            Easy = definition.Easy,
            Seasonal = definition.Seasonal,
            SecondsLeft = definition.Seasonal ? definition.SeasonalSeconds : 0,
        };
    }
}
