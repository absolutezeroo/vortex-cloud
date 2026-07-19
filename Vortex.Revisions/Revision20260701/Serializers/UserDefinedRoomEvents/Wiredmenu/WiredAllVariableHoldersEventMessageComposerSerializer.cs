using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Object;
using Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredAllVariableHoldersEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredAllVariableHoldersEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredAllVariableHoldersEventMessageComposer message
    )
    {
        packet.WriteInteger(0);

        WiredVariableSerializer.Serialize(packet, message.VariableSnapshot);

        packet.WriteInteger(message.ObjectValues.Count);

        foreach ((RoomObjectId objectId, int value) in message.ObjectValues)
        {
            packet.WriteInteger(objectId).WriteInteger(value);
        }
    }
}
