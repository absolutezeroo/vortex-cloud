using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Availability;

namespace Vortex.Revisions.Revision20260701.Serializers.Availability;

internal class MaintenanceStatusMessageComposerSerializer(int header)
    : AbstractSerializer<MaintenanceStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MaintenanceStatusMessageComposer message
    )
    {
        packet
            .WriteBoolean(message.IsInMaintenance)
            .WriteInteger(message.MinutesUntilMaintenance)
            .WriteInteger(message.DurationMinutes);
    }
}
