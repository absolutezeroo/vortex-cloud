using System.Collections.Immutable;
using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Parsers.Moderator;

internal class ReleaseIssuesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int count = packet.PopInt();
        ImmutableArray<int>.Builder issueIds = ImmutableArray.CreateBuilder<int>(count);

        for (int i = 0; i < count; i++)
        {
            issueIds.Add(packet.PopInt());
        }

        return new ReleaseIssuesMessage { IssueIds = issueIds.MoveToImmutable() };
    }
}
