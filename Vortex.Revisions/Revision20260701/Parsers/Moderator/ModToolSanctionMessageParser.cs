using Vortex.Protocol.Messages.Incoming.Moderator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class ModToolSanctionMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int issueId = packet.PopInt();
        int accountId = packet.PopInt();
        int categoryId = packet.PopInt();

        return new ModToolSanctionMessage
        {
            IssueId = issueId,
            AccountId = accountId,
            CategoryId = categoryId,
        };
    }
}
