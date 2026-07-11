using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Data;

namespace Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

internal class WiredFurniSelectorEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredFurniSelectorEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredFurniSelectorEventMessageComposer message
    )
    {
        WiredDataSerializer.Serialize(packet, message.WiredData);
    }
}
