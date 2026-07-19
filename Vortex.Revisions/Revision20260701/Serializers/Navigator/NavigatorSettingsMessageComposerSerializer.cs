using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class NavigatorSettingsMessageComposerSerializer(int header)
    : AbstractSerializer<NavigatorSettingsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NavigatorSettingsMessageComposer message
    )
    {
        packet.WriteInteger(message.HomeRoomId).WriteInteger(message.RoomIdToEnter);
    }
}
