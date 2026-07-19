using Vortex.Primitives.Messages.Outgoing.Room.Chat;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Chat;

internal class RoomChatSettingsMessageComposerSerializer(int header)
    : AbstractSerializer<RoomChatSettingsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomChatSettingsMessageComposer message)
    {
        //
    }
}
