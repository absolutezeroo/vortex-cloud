using Vortex.Primitives.Messages.Outgoing.Room.Bots;
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
        //
    }
}
