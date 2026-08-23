using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Achievements;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Achievements;

internal class AchievementEventMessageComposerSerializer(int header)
    : AbstractSerializer<AchievementEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, AchievementEventMessageComposer message)
    {
        AchievementDataWriter.Write(packet, message.Achievement);
    }
}
