using Vortex.Primitives.Messages.Outgoing.Handshake;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class UniqueMachineIdMessageSerializer(int header)
    : AbstractSerializer<UniqueMachineIdMessage>(header)
{
    protected override void Serialize(IServerPacket packet, UniqueMachineIdMessage message)
    {
        //
    }
}
