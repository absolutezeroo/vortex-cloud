using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class OneWayDoorStatusMessageComposerSerializer(int header)
    : AbstractSerializer<OneWayDoorStatusMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, OneWayDoorStatusMessageComposer message)
    {
        packet.WriteInteger(message.FurniId).WriteInteger(message.Status);
    }
}
