using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Navigator;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class FlatCreatedMessageComposerSerializer(int header)
    : AbstractSerializer<FlatCreatedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, FlatCreatedMessageComposer message)
    {
        packet.WriteInteger(message.RoomId).WriteString(message.Name);
    }
}
