using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Data;

namespace Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

internal class WiredFurniConditionEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredFurniConditionEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredFurniConditionEventMessageComposer message
    )
    {
        WiredDataSerializer.Serialize(packet, message.WiredData);
    }
}
