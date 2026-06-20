using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Groupforums;

internal class GetUnreadForumsCountMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetUnreadForumsCountMessage();
}
