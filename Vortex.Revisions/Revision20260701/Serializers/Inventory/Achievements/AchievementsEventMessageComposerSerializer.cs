using Vortex.Primitives.Packets;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Inventory.Achievements;

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
