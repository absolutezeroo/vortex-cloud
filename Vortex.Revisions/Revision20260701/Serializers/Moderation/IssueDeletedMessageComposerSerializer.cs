using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class IssueDeletedMessageComposerSerializer(int header)
    : AbstractSerializer<IssueDeletedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, IssueDeletedMessageComposer message)
    {
        //
    }
}
