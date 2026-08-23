using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Navigator;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class ConvertedRoomIdMessageComposerSerializer(int header)
    : AbstractSerializer<ConvertedRoomIdMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ConvertedRoomIdMessageComposer message)
    {
        packet.WriteString(message.GlobalId).WriteInteger(message.ConvertedId);
    }
}
