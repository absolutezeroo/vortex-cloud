using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredPermissionsEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredPermissionsEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredPermissionsEventMessageComposer message
    )
    {
        packet.WriteBoolean(message.CanModify).WriteBoolean(message.CanRead);
    }
}
