using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class FavoriteMembershipUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<FavoriteMembershipUpdateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FavoriteMembershipUpdateMessageComposer message
    )
    {
        //
    }
}
