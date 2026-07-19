using Vortex.Primitives.Messages.Outgoing.Roomsettings;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class MuteAllInRoomEventMessageComposerSerializer(int header)
    : AbstractSerializer<MuteAllInRoomEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        MuteAllInRoomEventMessageComposer message
    )
    {
        //
    }
}
