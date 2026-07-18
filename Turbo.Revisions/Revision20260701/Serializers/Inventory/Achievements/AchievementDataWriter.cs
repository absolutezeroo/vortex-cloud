using Turbo.Primitives.Packets;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.Revisions.Revision20260701.Serializers.Inventory.Achievements;

/// <summary>
/// Writes a single AchievementData block, shared by the achievements-list and single-achievement
/// composers. Field order matches the WIN63 20260701 client (14 fields, cumulative figures).
/// </summary>
internal static class AchievementDataWriter
{
    public static void Write(IServerPacket packet, AchievementProgressSnapshot achievement)
    {
        packet.WriteInteger(achievement.AchievementId);
        packet.WriteInteger(achievement.Level);
        packet.WriteString(achievement.BadgeCode);
        packet.WriteInteger(achievement.ScoreAtStartOfLevel);
        packet.WriteInteger(achievement.LevelMaxScore);
        packet.WriteInteger(achievement.LevelRewardAmount);
        packet.WriteInteger(achievement.LevelRewardType);
        packet.WriteInteger(achievement.CurrentProgress);
        packet.WriteBoolean(achievement.FinalLevel);
        packet.WriteString(achievement.Category);
        packet.WriteString(achievement.SubCategory);
        packet.WriteInteger(achievement.LevelCount);
        packet.WriteInteger(achievement.DisplayMethod);
        packet.WriteShort((short)achievement.State);
    }
}
