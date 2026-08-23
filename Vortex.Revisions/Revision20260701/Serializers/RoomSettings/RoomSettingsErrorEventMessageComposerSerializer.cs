using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Roomsettings;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class RoomSettingsErrorEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomSettingsErrorEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomSettingsErrorEventMessageComposer message
    )
    {
        //
    }
}
