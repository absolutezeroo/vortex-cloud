using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Chat;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Chat;

internal class UserTypingMessageComposerSerializer(int header)
    : AbstractSerializer<UserTypingMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserTypingMessageComposer message)
    {
        packet.WriteInteger(message.UserId).WriteInteger(message.IsTyping ? 1 : 0);
    }
}
