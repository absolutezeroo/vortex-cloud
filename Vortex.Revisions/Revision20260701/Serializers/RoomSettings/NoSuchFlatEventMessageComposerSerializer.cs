using Vortex.Primitives.Messages.Outgoing.Roomsettings;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class NoSuchFlatEventMessageComposerSerializer(int header)
    : AbstractSerializer<NoSuchFlatEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, NoSuchFlatEventMessageComposer message)
    {
        //
    }
}
