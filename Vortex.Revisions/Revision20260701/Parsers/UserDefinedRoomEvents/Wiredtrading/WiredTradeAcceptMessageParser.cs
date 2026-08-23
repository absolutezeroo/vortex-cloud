using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

internal class WiredTradeAcceptMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new WiredTradeAcceptMessage { Confirm = packet.PopBoolean() };
}
