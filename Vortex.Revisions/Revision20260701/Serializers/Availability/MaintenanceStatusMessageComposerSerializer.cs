using Vortex.Primitives.Messages.Outgoing.Availability;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Availability;

internal class MaintenanceStatusMessageComposerSerializer(int header)
    : AbstractSerializer<MaintenanceStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MaintenanceStatusMessageComposer message
    )
    {
        //
    }
}
