using Vortex.Primitives.Messages.Outgoing.Room.Layout;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Layout;

internal class RoomEntryTileMessageComposerSerializer(int header)
    : AbstractSerializer<RoomEntryTileMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomEntryTileMessageComposer message)
    {
        packet.WriteInteger(message.X).WriteInteger(message.Y).WriteInteger((int)message.Rotation);
    }
}
