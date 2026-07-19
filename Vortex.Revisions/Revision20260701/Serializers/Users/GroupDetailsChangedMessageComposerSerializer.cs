using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class GroupDetailsChangedMessageComposerSerializer(int header)
    : AbstractSerializer<GroupDetailsChangedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GroupDetailsChangedMessageComposer message
    )
    {
        packet.WriteInteger(message.GroupId);
    }
}
