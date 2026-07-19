using Vortex.Primitives.Messages.Outgoing.Notifications;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Notifications;

internal class HabboAchievementNotificationMessageComposerSerializer(int header)
    : AbstractSerializer<HabboAchievementNotificationMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        HabboAchievementNotificationMessageComposer message
    )
    {
        packet.WriteInteger(message.Type);
        packet.WriteInteger(message.Level);
        packet.WriteInteger(message.BadgeId);
        packet.WriteString(message.BadgeCode);
        packet.WriteInteger(message.Points);
        packet.WriteInteger(message.LevelRewardPoints);
        packet.WriteInteger(message.LevelRewardPointType);
        packet.WriteInteger(message.BonusPoints);
        packet.WriteInteger(message.AchievementId);
        packet.WriteString(message.RemovedBadgeCode);
        packet.WriteString(message.Category);
        packet.WriteBoolean(message.ShowDialogToUser);
        packet.WriteInteger(message.OwnerCount);
        packet.WriteInteger(message.BadgeRarityId);
    }
}
