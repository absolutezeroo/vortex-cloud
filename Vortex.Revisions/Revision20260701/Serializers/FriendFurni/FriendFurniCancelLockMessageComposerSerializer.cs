using Vortex.Primitives.Messages.Outgoing.Friendfurni;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendFurni;

internal class FriendFurniCancelLockMessageComposerSerializer(int header)
    : AbstractSerializer<FriendFurniCancelLockMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FriendFurniCancelLockMessageComposer message
    )
    {
        //
    }
}
