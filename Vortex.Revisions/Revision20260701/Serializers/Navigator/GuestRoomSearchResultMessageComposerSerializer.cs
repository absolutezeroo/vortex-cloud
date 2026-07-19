using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class GuestRoomSearchResultMessageComposerSerializer(int header)
    : AbstractSerializer<GuestRoomSearchResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuestRoomSearchResultMessageComposer message
    )
    {
        //
    }
}
