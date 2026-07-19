using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class GroupMembershipRequestedMessageComposerSerializer(int header)
    : AbstractSerializer<GroupMembershipRequestedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GroupMembershipRequestedMessageComposer message
    )
    {
        //
    }
}
