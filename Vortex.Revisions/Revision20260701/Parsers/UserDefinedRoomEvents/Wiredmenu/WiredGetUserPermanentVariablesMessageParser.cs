using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredGetUserPermanentVariablesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new WiredGetUserPermanentVariablesMessage()
        {
            EntityType = packet.PopInt(),
            EntityId = packet.PopInt(),
        };
}
