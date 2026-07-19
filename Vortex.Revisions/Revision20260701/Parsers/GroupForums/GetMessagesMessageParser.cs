using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.GroupForums;

internal class GetMessagesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetMessagesMessage
        {
            GroupId = packet.PopInt(),
            ThreadId = packet.PopInt(),
            StartIndex = packet.PopInt(),
            Amount = packet.PopInt(),
        };
}
