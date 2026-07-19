using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Action;

internal class DanceMessageComposerSerializer(int header)
    : AbstractSerializer<DanceMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, DanceMessageComposer message)
    {
        packet.WriteInteger(message.ObjectId).WriteInteger((int)message.DanceType);
    }
}
