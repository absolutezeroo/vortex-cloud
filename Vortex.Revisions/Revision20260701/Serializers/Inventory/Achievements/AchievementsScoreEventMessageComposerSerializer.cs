using Vortex.Primitives.Messages.Outgoing.Inventory.Achievements;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Achievements;

internal class AchievementsScoreEventMessageComposerSerializer(int header)
    : AbstractSerializer<AchievementsScoreEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AchievementsScoreEventMessageComposer message
    )
    {
        packet.WriteInteger(message.Score);
    }
}
