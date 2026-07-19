using Vortex.Primitives.Messages.Outgoing.Roomsettings;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class UserUnbannedFromRoomEventMessageComposerSerializer(int header)
    : AbstractSerializer<UserUnbannedFromRoomEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        UserUnbannedFromRoomEventMessageComposer message
    )
    {
        //
    }
}
