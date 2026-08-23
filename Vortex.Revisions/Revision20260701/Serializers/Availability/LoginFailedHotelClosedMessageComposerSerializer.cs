using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Availability;

namespace Vortex.Revisions.Revision20260701.Serializers.Availability;

internal class LoginFailedHotelClosedMessageComposerSerializer(int header)
    : AbstractSerializer<LoginFailedHotelClosedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        LoginFailedHotelClosedMessageComposer message
    )
    {
        packet.WriteInteger(message.OpenHour).WriteInteger(message.OpenMinute);
    }
}
