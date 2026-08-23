using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredGetRoomLogsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new WiredGetRoomLogsMessage()
        {
            Page = packet.PopInt(),
            PageSize = packet.PopInt(),
            LogLevelFilter = packet.PopInt(),
            LogSourceFilter = packet.PopInt(),
            Query = packet.PopString(),
        };
}
