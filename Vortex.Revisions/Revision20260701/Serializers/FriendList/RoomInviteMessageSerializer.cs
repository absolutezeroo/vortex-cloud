using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class RoomInviteMessageSerializer(int header)
    : AbstractSerializer<RoomInviteMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomInviteMessageComposer message)
    {
        packet.WriteInteger(message.SenderId);
        packet.WriteString(message.Message);
    }
}
