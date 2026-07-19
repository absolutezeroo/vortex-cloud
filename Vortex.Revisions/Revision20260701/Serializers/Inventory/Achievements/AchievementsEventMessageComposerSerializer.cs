using Vortex.Primitives.Messages.Outgoing.Inventory.Achievements;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Achievements;

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
