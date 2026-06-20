using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Groupforums;

internal class GetThreadMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetThreadMessage { GroupId = packet.PopInt(), ThreadId = packet.PopInt() };
}
