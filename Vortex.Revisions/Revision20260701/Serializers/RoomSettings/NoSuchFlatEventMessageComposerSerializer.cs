using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Roomsettings;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class NoSuchFlatEventMessageComposerSerializer(int header)
    : AbstractSerializer<NoSuchFlatEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, NoSuchFlatEventMessageComposer message)
    {
        //
    }
}
