using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents;

internal class OpenMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new OpenMessage { Id = packet.PopInt() };
}
