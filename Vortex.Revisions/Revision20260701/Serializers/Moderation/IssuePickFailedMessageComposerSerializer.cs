using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class IssuePickFailedMessageComposerSerializer(int header)
    : AbstractSerializer<IssuePickFailedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, IssuePickFailedMessageComposer message)
    {
        //
    }
}
