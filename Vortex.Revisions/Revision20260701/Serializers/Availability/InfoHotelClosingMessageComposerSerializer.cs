using Vortex.Primitives.Messages.Outgoing.Availability;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Availability;

internal class InfoHotelClosingMessageComposerSerializer(int header)
    : AbstractSerializer<InfoHotelClosingMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, InfoHotelClosingMessageComposer message)
    {
        //
    }
}
