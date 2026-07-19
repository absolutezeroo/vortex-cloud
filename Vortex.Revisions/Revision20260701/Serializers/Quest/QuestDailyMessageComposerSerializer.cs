using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

internal class QuestDailyMessageComposerSerializer(int header)
    : AbstractSerializer<QuestDailyMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, QuestDailyMessageComposer message)
    {
        bool hasDaily = message.Quest is not null;
        packet.WriteBoolean(hasDaily);

        if (hasDaily)
        {
            QuestDataWriter.Write(packet, message.Quest!);
            packet.WriteInteger(message.EasyQuestCount);
            packet.WriteInteger(message.HardQuestCount);
        }
    }
}
