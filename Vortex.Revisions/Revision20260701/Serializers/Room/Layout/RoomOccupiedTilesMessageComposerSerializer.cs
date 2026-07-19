using Vortex.Primitives.Messages.Outgoing.Room.Layout;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Layout;

internal class RoomOccupiedTilesMessageComposerSerializer(int header)
    : AbstractSerializer<RoomOccupiedTilesMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomOccupiedTilesMessageComposer message
    )
    {
        //
    }
}
