using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.GroupForums;

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
