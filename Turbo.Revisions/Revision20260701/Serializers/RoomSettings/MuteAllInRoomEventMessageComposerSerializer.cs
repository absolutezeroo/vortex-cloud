using Turbo.Primitives.Messages.Outgoing.Roomsettings;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.RoomSettings;

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
