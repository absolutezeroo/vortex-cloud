using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Moderation;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class RoomChatlogEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomChatlogEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomChatlogEventMessageComposer message)
    {
        ChatlogSerialization.WriteBlock(packet, message.Block);
    }
}
