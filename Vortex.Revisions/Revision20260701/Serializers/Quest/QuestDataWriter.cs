using Vortex.Primitives.Packets;
using Vortex.Primitives.Quests.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

/// <summary>
/// Writes a single QuestData block, shared by the quest-list, single-quest and daily composers.
/// Field order matches the WIN63 client; the trailing seconds-left int is only written for seasonal
/// quests.
/// </summary>
internal static class QuestDataWriter
{
    public static void Write(IServerPacket packet, QuestSnapshot quest)
    {
        packet.WriteString(quest.CampaignCode);
        packet.WriteInteger(quest.CompletedQuestsInCampaign);
        packet.WriteInteger(quest.QuestCountInCampaign);
        packet.WriteInteger(quest.ActivityPointType);
        packet.WriteInteger(quest.Id);
        packet.WriteBoolean(quest.Accepted);
        packet.WriteString(quest.QuestType);
        packet.WriteString(quest.ImageVersion);
        packet.WriteInteger(quest.RewardCurrencyAmount);
        packet.WriteString(quest.LocalizationCode);
        packet.WriteInteger(quest.CompletedSteps);
        packet.WriteInteger(quest.TotalSteps);
        packet.WriteInteger(quest.SortOrder);
        packet.WriteString(quest.CatalogPageName);
        packet.WriteString(quest.ChainCode);
        packet.WriteBoolean(quest.Easy);
        packet.WriteBoolean(quest.Seasonal);

        if (quest.Seasonal)
        {
            packet.WriteInteger(quest.SecondsLeft);
        }
    }
}
