using Vortex.Primitives.Messages.Outgoing.Handshake;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class GenericErrorMessageSerializer(int header)
    : AbstractSerializer<GenericErrorMessage>(header)
{
    protected override void Serialize(IServerPacket packet, GenericErrorMessage message)
    {
        packet.WriteInteger((int)message.ErrorCode);
    }
}
