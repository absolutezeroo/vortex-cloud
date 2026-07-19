using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredmenu;

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
