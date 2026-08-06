using System.Collections.Immutable;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class CallForHelpFromIMMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        string message = packet.PopString();
        int topicId = packet.PopInt();
        int reportedUserId = packet.PopInt();

        // The client pushes the flat (userId, text) array and sends length/2 as the count, exactly
        // as the room-based CallForHelp does.
        int evidenceCount = packet.PopInt();
        ImmutableArray<CfhEvidenceLine>.Builder evidence =
            ImmutableArray.CreateBuilder<CfhEvidenceLine>(evidenceCount);

        for (int i = 0; i < evidenceCount; i++)
        {
            evidence.Add(new CfhEvidenceLine(packet.PopInt(), packet.PopString()));
        }

        return new CallForHelpFromIMMessage
        {
            Message = message,
            TopicId = topicId,
            ReportedUserId = reportedUserId,
            Evidence = evidence.MoveToImmutable(),
        };
    }
}
