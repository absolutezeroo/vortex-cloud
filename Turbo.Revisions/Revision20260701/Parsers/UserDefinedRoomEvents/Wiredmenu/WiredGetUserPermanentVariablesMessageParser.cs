using Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredGetUserPermanentVariablesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new WiredGetUserPermanentVariablesMessage()
        {
            EntityType = packet.PopInt(),
            EntityId = packet.PopInt(),
        };
}
