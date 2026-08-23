using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Handshake;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class UniqueMachineIdMessageSerializer(int header)
    : AbstractSerializer<UniqueMachineIdMessage>(header)
{
    protected override void Serialize(IServerPacket packet, UniqueMachineIdMessage message)
    {
        packet.WriteString(message.MachineID);
    }
}
