using Turbo.Primitives.Messages.Outgoing.Inventory.Achievements;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.Revisions.Revision20260701.Serializers.Inventory.Achievements;

internal class AchievementsEventMessageComposerSerializer(int header)
    : AbstractSerializer<AchievementsEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AchievementsEventMessageComposer message
    )
    {
        packet.WriteInteger(message.Achievements.Length);

        foreach (AchievementProgressSnapshot achievement in message.Achievements)
        {
            AchievementDataWriter.Write(packet, achievement);
        }

        packet.WriteString(message.DefaultCategory);
    }
}
