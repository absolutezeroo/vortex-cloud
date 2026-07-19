using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class RoomChatlogEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomChatlogEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomChatlogEventMessageComposer message)
    {
        ChatlogSerialization.WriteBlock(packet, message.Block);
    }
}
