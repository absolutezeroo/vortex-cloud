using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class FlatCreatedMessageComposerSerializer(int header)
    : AbstractSerializer<FlatCreatedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, FlatCreatedMessageComposer message)
    {
        packet.WriteInteger(message.RoomId).WriteString(message.Name);
    }
}
