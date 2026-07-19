using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

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
