using Vortex.Primitives.Messages.Outgoing.Nux;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Nux;

internal class SelectInitialRoomEventMessageComposerSerializer(int header)
    : AbstractSerializer<SelectInitialRoomEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        SelectInitialRoomEventMessageComposer message
    )
    {
        //
    }
}
