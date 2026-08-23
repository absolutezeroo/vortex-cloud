using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Navigator;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class DoorbellMessageComposerSerializer(int header)
    : AbstractSerializer<DoorbellMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, DoorbellMessageComposer message)
    {
        packet.WriteString(message.Username ?? string.Empty);
    }
}
