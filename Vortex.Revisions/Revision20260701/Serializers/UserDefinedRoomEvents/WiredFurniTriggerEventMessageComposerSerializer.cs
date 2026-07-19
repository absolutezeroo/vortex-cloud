using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

internal class WiredFurniTriggerEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredFurniTriggerEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredFurniTriggerEventMessageComposer message
    )
    {
        WiredDataSerializer.Serialize(packet, message.WiredData);
    }
}
