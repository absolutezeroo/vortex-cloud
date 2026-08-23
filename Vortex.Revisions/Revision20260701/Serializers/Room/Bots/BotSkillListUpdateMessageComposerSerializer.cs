using Vortex.Protocol.Messages.Outgoing.Room.Bots;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Bots;

internal class BotSkillListUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<BotSkillListUpdateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BotSkillListUpdateMessageComposer message
    )
    {
        packet.WriteInteger(message.BotId).WriteInteger(message.Skills.Length);

        foreach (BotSkillEntry skill in message.Skills)
        {
            packet.WriteInteger(skill.CommandId).WriteString(skill.Data);
        }
    }
}
