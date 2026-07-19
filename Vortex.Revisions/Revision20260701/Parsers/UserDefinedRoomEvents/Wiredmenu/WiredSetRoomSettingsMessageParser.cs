using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredSetRoomSettingsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new WiredSetRoomSettingsMessage()
        {
            ModifyPermissionMask = packet.PopInt(),
            ReadPermissionMask = packet.PopInt(),
            Timezone = packet.PopString(),
        };
}
