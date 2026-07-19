using Vortex.Primitives.Messages.Outgoing.Room.Chat;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Chat;

internal class RoomFilterSettingsMessageComposerSerializer(int header)
    : AbstractSerializer<RoomFilterSettingsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomFilterSettingsMessageComposer message
    )
    {
        //
    }
}
