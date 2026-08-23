using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Chat;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Chat;

internal class FloodControlMessageComposerSerializer(int header)
    : AbstractSerializer<FloodControlMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, FloodControlMessageComposer message)
    {
        packet.WriteInteger(message.Seconds);
    }
}
