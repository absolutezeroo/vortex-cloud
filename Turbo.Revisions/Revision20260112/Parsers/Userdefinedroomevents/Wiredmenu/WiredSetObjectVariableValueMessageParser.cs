using Turbo.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredSetObjectVariableValueMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new WiredSetObjectVariableValueMessage()
        {
            EntityType = packet.PopInt(),
            EntityId = packet.PopInt(),
            VariableId = packet.PopString(),
            Value = packet.PopInt(),
            Action = packet.PopInt(),
        };
}
