using Vortex.Primitives.Messages.Outgoing.Advertisement;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Advertisement;

internal class RoomAdErrorEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomAdErrorEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomAdErrorEventMessageComposer message)
    {
        //
    }
}
