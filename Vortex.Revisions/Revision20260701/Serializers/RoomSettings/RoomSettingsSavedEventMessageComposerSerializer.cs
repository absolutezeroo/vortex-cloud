using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Roomsettings;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class RoomSettingsSavedEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomSettingsSavedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomSettingsSavedEventMessageComposer message
    )
    {
        packet.WriteInteger(message.RoomId);
    }
}
