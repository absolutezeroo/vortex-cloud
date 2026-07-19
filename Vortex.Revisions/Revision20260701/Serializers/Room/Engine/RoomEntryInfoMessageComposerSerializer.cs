using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class RoomEntryInfoMessageComposerSerializer(int header)
    : AbstractSerializer<RoomEntryInfoMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomEntryInfoMessageComposer message)
    {
        packet.WriteInteger(message.RoomId).WriteBoolean(message.IsOwner);
    }
}
