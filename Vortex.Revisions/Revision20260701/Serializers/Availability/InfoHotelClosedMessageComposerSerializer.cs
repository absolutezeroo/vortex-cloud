using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Availability;

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
