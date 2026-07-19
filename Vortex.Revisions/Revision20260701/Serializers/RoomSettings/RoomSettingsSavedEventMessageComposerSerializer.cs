using Vortex.Primitives.Messages.Outgoing.Roomsettings;
using Vortex.Primitives.Packets;

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
