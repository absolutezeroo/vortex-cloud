using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class UserBannedMessageComposerSerializer(int header)
    : AbstractSerializer<UserBannedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserBannedMessageComposer message)
    {
        packet.WriteString(message.Message);
    }
}
