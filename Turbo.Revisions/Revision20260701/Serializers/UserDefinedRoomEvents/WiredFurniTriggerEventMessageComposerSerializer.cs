using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Data;

namespace Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

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
