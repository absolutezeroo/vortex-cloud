using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Groupforums;

internal class PostMessageMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PostMessageMessage
        {
            GroupId = packet.PopInt(),
            ThreadId = packet.PopInt(),
            Title = packet.PopString(),
            Message = packet.PopString(),
        };
}
