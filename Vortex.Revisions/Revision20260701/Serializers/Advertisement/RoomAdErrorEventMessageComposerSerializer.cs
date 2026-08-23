using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Advertisement;

namespace Vortex.Revisions.Revision20260701.Serializers.Advertisement;

internal class RoomAdErrorEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomAdErrorEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomAdErrorEventMessageComposer message)
    {
        packet.WriteInteger(message.ErrorCode).WriteString(message.FilteredText);
    }
}
