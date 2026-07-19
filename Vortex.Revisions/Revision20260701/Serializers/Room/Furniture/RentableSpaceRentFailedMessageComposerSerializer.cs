using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Packets;

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
