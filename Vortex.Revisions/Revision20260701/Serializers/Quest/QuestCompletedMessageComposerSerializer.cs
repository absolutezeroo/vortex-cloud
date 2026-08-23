using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Quest;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

internal class QuestCompletedMessageComposerSerializer(int header)
    : AbstractSerializer<QuestCompletedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, QuestCompletedMessageComposer message)
    {
        QuestDataWriter.Write(packet, message.Quest);
        packet.WriteBoolean(message.ShowDialog);
    }
}
