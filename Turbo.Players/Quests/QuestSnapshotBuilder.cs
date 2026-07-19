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
        int questCountInCampaign,
        DateTime now
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
            SecondsLeft = SecondsLeftFor(definition, now),
        };
    }

    /// <summary>
    /// Seconds remaining for a seasonal quest: counts down to <see cref="QuestDefinitionSnapshot.EndsAt"/>
    /// when set (clamped at 0), else the static <see cref="QuestDefinitionSnapshot.SeasonalSeconds"/>.
    /// Non-seasonal quests are always 0.
    /// </summary>
    private static int SecondsLeftFor(QuestDefinitionSnapshot definition, DateTime now)
    {
        if (!definition.Seasonal)
        {
            return 0;
        }

        if (definition.EndsAt is DateTime endsAt)
        {
            double remaining = (endsAt - now).TotalSeconds;
            return remaining <= 0 ? 0 : (int)remaining;
        }

        return definition.SeasonalSeconds;
    }
}
