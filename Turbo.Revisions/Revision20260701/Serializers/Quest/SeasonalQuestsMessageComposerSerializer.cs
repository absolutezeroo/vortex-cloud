using Turbo.Primitives.Messages.Outgoing.Quest;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.Revisions.Revision20260701.Serializers.Quest;

internal class SeasonalQuestsMessageComposerSerializer(int header)
    : AbstractSerializer<SeasonalQuestsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, SeasonalQuestsMessageComposer message)
    {
        packet.WriteInteger(message.Quests.Length);

        foreach (QuestSnapshot quest in message.Quests)
        {
            QuestDataWriter.Write(packet, quest);
        }
    }
}
