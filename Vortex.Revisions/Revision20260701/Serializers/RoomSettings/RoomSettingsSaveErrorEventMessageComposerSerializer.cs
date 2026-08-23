using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Roomsettings;

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
