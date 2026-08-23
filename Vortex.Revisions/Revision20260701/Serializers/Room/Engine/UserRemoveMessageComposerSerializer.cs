using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class UserRemoveMessageComposerSerializer(int header)
    : AbstractSerializer<UserRemoveMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserRemoveMessageComposer message)
    {
        packet.WriteString(message.ObjectId.ToString());
    }
}
