using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredRoomSettingsEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredRoomSettingsEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredRoomSettingsEventMessageComposer message
    )
    {
        packet
            .WriteInteger(message.ModifyPermissionMask)
            .WriteInteger(message.ReadPermissionMask)
            .WriteString(message.Timezone);
    }
}
