using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class RoomVisitsEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomVisitsEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomVisitsEventMessageComposer message)
    {
        //
    }
}
