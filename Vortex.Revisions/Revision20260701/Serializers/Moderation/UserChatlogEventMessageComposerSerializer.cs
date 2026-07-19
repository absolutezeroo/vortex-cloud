using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class UserChatlogEventMessageComposerSerializer(int header)
    : AbstractSerializer<UserChatlogEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserChatlogEventMessageComposer message)
    {
        packet
            .WriteInteger(message.UserId)
            .WriteString(message.UserName)
            .WriteInteger(message.Rooms.Length);

        foreach (ChatlogBlockSnapshot block in message.Rooms)
        {
            ChatlogSerialization.WriteBlock(packet, block);
        }
    }
}
