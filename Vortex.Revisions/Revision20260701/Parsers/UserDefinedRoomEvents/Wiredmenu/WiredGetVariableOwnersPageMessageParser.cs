using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredGetVariableOwnersPageMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new WiredGetVariableOwnersPageMessage()
        {
            VariableId = packet.PopString(),
            Page = packet.PopInt(),
            PageSize = packet.PopInt(),
            UserTypeFilter = packet.PopInt(),
            SortTypeFilter = packet.PopInt(),
        };
}
