using Vortex.Primitives.Messages.Outgoing.Roomsettings;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class FlatControllerRemovedEventMessageComposerSerializer(int header)
    : AbstractSerializer<FlatControllerRemovedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FlatControllerRemovedEventMessageComposer message
    )
    {
        //
    }
}
