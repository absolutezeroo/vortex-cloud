using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.FriendList;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class MessengerErrorMessageSerializer(int header)
    : AbstractSerializer<MessengerErrorMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, MessengerErrorMessageComposer message)
    {
        packet.WriteInteger(message.ClientMessageId);
        packet.WriteInteger((int)message.ErrorCode);
    }
}
