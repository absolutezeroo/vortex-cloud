using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

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
