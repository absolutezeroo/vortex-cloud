using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class RentableSpaceRentOkMessageComposerSerializer(int header)
    : AbstractSerializer<RentableSpaceRentOkMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RentableSpaceRentOkMessageComposer message
    )
    {
        packet.WriteInteger(message.ExpiryTime);
    }
}
