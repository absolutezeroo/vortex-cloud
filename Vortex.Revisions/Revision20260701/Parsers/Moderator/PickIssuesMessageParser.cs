using System.Collections.Immutable;
using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class PickIssuesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int count = packet.PopInt();
        ImmutableArray<int>.Builder issueIds = ImmutableArray.CreateBuilder<int>(count);

        for (int i = 0; i < count; i++)
        {
            issueIds.Add(packet.PopInt());
        }

        return new PickIssuesMessage
        {
            IssueIds = issueIds.MoveToImmutable(),
            AutoHandle = packet.PopBoolean(),
            RoomId = packet.PopInt(),
            Note = packet.PopString(),
        };
    }
}
