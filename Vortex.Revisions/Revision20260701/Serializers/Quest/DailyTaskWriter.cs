using Vortex.Primitives.Packets;
using Vortex.Primitives.Quests.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

/// <summary>
/// Writes one daily-task block, shared by the active-list and tasks-added composers because the
/// client parses both with the same struct constructor.
/// </summary>
/// <remarks>
/// Two field widths are not the obvious ones and both would corrupt everything after them: the id
/// is a <b>long</b> (the client reads it with readLong and keeps it as a Number), the status is a
/// <b>byte</b>, and inside a reward the product item type is a <b>short</b>.
/// </remarks>
internal static class DailyTaskWriter
{
    public static void Write(IServerPacket packet, DailyTaskSnapshot task)
    {
        packet
            .WriteLong(task.TaskId)
            .WriteString(task.TaskCode)
            .WriteString(task.QuestTypeCode)
            .WriteBoolean(task.IsBonus)
            .WriteString(task.ImageVersion)
            .WriteString(task.CatalogName)
            .WriteInteger(task.RequiredRepeats)
            .WriteInteger(task.Repeats)
            .WriteByte((byte)task.Status)
            .WriteInteger(task.SecondsLeft)
            .WriteInteger(task.Rewards.Length);

        foreach (DailyTaskRewardSnapshot reward in task.Rewards)
        {
            packet
                .WriteShort(reward.ProductItemTypeId)
                .WriteString(reward.RewardTypeId)
                .WriteString(reward.ExtraParams)
                .WriteInteger(reward.Amount);
        }
    }
}
