using Vortex.Primitives.Messages.Outgoing.Friendfurni;
using Vortex.Primitives.Packets;

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
