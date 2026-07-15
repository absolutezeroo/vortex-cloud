using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.UserDefinedRoomEvents.Wiredmenu;

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
