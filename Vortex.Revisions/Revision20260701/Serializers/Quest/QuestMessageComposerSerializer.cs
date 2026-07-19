using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

internal class QuestMessageComposerSerializer(int header)
    : AbstractSerializer<QuestMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, QuestMessageComposer message)
    {
        QuestDataWriter.Write(packet, message.Quest);
    }
}
