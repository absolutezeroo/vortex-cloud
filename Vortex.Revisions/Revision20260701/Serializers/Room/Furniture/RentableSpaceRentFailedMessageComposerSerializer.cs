using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class RentableSpaceRentFailedMessageComposerSerializer(int header)
    : AbstractSerializer<RentableSpaceRentFailedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RentableSpaceRentFailedMessageComposer message
    )
    {
        packet.WriteInteger((int)message.Reason);
    }
}
