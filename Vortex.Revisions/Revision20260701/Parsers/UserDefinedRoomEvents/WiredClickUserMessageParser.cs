using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents;

internal class WiredClickUserMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new WiredClickUserMessage { ObjectId = packet.PopInt() };
}
