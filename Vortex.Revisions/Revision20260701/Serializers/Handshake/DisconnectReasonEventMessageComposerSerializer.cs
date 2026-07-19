using Vortex.Primitives.Messages.Outgoing.Handshake;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class DisconnectReasonEventMessageComposerSerializer(int header)
    : AbstractSerializer<DisconnectReasonEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        DisconnectReasonEventMessageComposer message
    )
    {
        //
    }
}
