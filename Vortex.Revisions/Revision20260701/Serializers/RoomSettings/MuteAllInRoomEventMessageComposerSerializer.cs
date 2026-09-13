using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Roomsettings;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class MuteAllInRoomEventMessageComposerSerializer(int header)
    : AbstractSerializer<MuteAllInRoomEventMessageComposer>(header)
{
    // One boolean, per the client's parser (unknowns/_SafePkg_2452/_SafeCls_2723.as:18).
    protected override void Serialize(
        IServerPacket packet,
        MuteAllInRoomEventMessageComposer message
    )
    {
        packet.WriteBoolean(message.AllMuted);
    }
}
