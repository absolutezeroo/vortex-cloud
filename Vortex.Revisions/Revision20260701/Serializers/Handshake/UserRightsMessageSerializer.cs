using Vortex.Primitives.Messages.Outgoing.Handshake;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class UserRightsMessageSerializer(int header)
    : AbstractSerializer<UserRightsMessage>(header)
{
    protected override void Serialize(IServerPacket packet, UserRightsMessage message)
    {
        packet.WriteInteger((int)message.ClubLevel);
        packet.WriteInteger((int)message.SecurityLevel);
        packet.WriteBoolean(message.IsAmbassador);
    }
}
