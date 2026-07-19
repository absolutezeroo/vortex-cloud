using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Action;

internal class UseObjectMessageComposerSerializer(int header)
    : AbstractSerializer<UseObjectMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UseObjectMessageComposer message)
    {
        packet.WriteInteger(message.UserId).WriteInteger(message.ItemType);
    }
}
