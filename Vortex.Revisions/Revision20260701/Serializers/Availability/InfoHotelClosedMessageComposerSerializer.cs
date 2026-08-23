using Vortex.Protocol.Messages.Outgoing.Availability;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Availability;

internal class InfoHotelClosedMessageComposerSerializer(int header)
    : AbstractSerializer<InfoHotelClosedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, InfoHotelClosedMessageComposer message)
    {
        packet
            .WriteInteger(message.OpenHour)
            .WriteInteger(message.OpenMinute)
            .WriteBoolean(message.UserThrownOutAtClose);
    }
}
