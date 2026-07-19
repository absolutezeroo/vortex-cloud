using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class InstantMessageErrorMessageSerializer(int header)
    : AbstractSerializer<InstantMessageErrorMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        InstantMessageErrorMessageComposer message
    )
    {
        packet.WriteInteger((int)message.ErrorCode);
        packet.WriteInteger(message.PlayerId);
        packet.WriteString(message.Message);
    }
}
