using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class RentableSpaceStatusMessageComposerSerializer(int header)
    : AbstractSerializer<RentableSpaceStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RentableSpaceStatusMessageComposer message
    )
    {
        packet
            .WriteBoolean(message.Rented)
            .WriteInteger((int)message.CanRentErrorCode)
            .WriteInteger(message.RenterId)
            .WriteString(message.RenterName)
            .WriteInteger(message.TimeRemaining)
            .WriteInteger(message.Price)
            .WriteString(message.CurrencyName);
    }
}
