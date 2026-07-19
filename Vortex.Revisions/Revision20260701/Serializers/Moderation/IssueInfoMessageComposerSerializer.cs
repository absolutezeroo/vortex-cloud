using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class IssueInfoMessageComposerSerializer(int header)
    : AbstractSerializer<IssueInfoMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, IssueInfoMessageComposer message)
    {
        //
    }
}
