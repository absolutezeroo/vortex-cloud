using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;

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
