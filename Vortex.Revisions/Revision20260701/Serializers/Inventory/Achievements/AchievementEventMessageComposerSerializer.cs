using Vortex.Primitives.Messages.Outgoing.Inventory.Achievements;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Achievements;

internal class AchievementEventMessageComposerSerializer(int header)
    : AbstractSerializer<AchievementEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, AchievementEventMessageComposer message)
    {
        AchievementDataWriter.Write(packet, message.Achievement);
    }
}
