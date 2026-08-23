using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Friendfurni;

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
