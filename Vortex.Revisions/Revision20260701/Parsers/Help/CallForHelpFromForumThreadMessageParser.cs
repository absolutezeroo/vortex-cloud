using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class CallForHelpFromForumThreadMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new CallForHelpFromForumThreadMessage();
}
