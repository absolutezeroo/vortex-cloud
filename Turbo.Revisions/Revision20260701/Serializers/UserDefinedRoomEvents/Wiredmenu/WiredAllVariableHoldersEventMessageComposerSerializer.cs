using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Object;
using Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Data;

namespace Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

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
