using System.Collections.Immutable;
using Turbo.Primitives.Messages.Incoming.Help;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Help;

internal class CallForHelpMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        string message = packet.PopString();
        int topicId = packet.PopInt();
        int reportedUserId = packet.PopInt();
        int roomId = packet.PopInt();

        int evidenceCount = packet.PopInt();
        ImmutableArray<CfhEvidenceLine>.Builder evidence =
            ImmutableArray.CreateBuilder<CfhEvidenceLine>(evidenceCount);

        for (int i = 0; i < evidenceCount; i++)
        {
            evidence.Add(new CfhEvidenceLine(packet.PopInt(), packet.PopString()));
        }

        string extra1 = packet.PopString();
        string extra2 = packet.PopString();

        return new CallForHelpMessage
        {
            Message = message,
            TopicId = topicId,
            ReportedUserId = reportedUserId,
            RoomId = roomId,
            Evidence = evidence.MoveToImmutable(),
            Extra1 = extra1,
            Extra2 = extra2,
        };
    }
}
