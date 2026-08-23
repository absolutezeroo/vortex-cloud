using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Friendfurni;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendFurni;

internal class FriendFurniOtherLockConfirmedMessageComposerSerializer(int header)
    : AbstractSerializer<FriendFurniOtherLockConfirmedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FriendFurniOtherLockConfirmedMessageComposer message
    )
    {
        //
    }
}
