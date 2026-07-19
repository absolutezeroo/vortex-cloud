using Vortex.Primitives.Messages.Outgoing.Roomsettings;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class FlatControllerAddedEventMessageComposerSerializer(int header)
    : AbstractSerializer<FlatControllerAddedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FlatControllerAddedEventMessageComposer message
    )
    {
        //
    }
}
