using Vortex.Primitives.Messages.Outgoing.Availability;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Availability;

internal class LoginFailedHotelClosedMessageComposerSerializer(int header)
    : AbstractSerializer<LoginFailedHotelClosedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        LoginFailedHotelClosedMessageComposer message
    )
    {
        //
    }
}
