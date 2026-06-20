using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260112.Serializers.UserDefinedRoomEvents.Data;

namespace Turbo.Revisions.Revision20260112.Serializers.UserDefinedRoomEvents;

internal class WiredFurniVariableEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredFurniVariableEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredFurniVariableEventMessageComposer message
    )
    {
        WiredDataSerializer.Serialize(packet, message.WiredData);
    }
}
