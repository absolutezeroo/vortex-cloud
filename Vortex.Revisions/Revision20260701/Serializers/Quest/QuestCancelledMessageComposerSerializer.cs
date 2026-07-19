using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

internal class QuestCancelledMessageComposerSerializer(int header)
    : AbstractSerializer<QuestCancelledMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, QuestCancelledMessageComposer message)
    {
        packet.WriteBoolean(message.Expired);
        QuestDataWriter.Write(packet, message.Quest);
    }
}
