using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Moderation;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class IssueInfoMessageComposerSerializer(int header)
    : AbstractSerializer<IssueInfoMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, IssueInfoMessageComposer message) =>
        IssueSerialization.WriteIssue(packet, message.Issue);
}
