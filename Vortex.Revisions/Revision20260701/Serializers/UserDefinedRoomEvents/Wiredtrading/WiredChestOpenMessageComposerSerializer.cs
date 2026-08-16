using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

internal class WiredChestOpenMessageComposerSerializer(int header)
    : AbstractSerializer<WiredChestOpenMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, WiredChestOpenMessageComposer message)
    {
        packet.WriteInteger(message.ChestId);
    }
}
