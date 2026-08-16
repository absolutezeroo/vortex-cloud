using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

internal class OpenWiredChestMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new OpenWiredChestMessage() { ChestId = packet.PopInt() };
}
