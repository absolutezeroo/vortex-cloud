using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Packets;

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
