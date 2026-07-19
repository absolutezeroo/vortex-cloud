using Vortex.Primitives.Messages.Outgoing.Roomsettings;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class RoomSettingsSaveErrorEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomSettingsSaveErrorEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomSettingsSaveErrorEventMessageComposer message
    )
    {
        //
    }
}
