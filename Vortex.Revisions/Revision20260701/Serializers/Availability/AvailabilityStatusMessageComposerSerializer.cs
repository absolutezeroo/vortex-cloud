using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Availability;

namespace Vortex.Revisions.Revision20260701.Serializers.Availability;

internal class AvailabilityStatusMessageComposerSerializer(int header)
    : AbstractSerializer<AvailabilityStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        AvailabilityStatusMessageComposer message
    )
    {
        packet
            .WriteBoolean(message.IsOpen)
            .WriteBoolean(message.OnShutDown)
            .WriteBoolean(message.IsAuthenticHabbo);
    }
}
