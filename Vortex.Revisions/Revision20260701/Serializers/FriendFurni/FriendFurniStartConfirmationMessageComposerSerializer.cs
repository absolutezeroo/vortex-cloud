using Vortex.Primitives.Messages.Outgoing.Friendfurni;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendFurni;

internal class FriendFurniStartConfirmationMessageComposerSerializer(int header)
    : AbstractSerializer<FriendFurniStartConfirmationMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FriendFurniStartConfirmationMessageComposer message
    )
    {
        //
    }
}
