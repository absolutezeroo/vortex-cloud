using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Quests.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

internal class QuestsMessageComposerSerializer(int header)
    : AbstractSerializer<QuestsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, QuestsMessageComposer message)
    {
        packet.WriteInteger(message.Quests.Length);

        foreach (QuestSnapshot quest in message.Quests)
        {
            QuestDataWriter.Write(packet, quest);
        }

        packet.WriteBoolean(message.OpenWindow);
    }
}
