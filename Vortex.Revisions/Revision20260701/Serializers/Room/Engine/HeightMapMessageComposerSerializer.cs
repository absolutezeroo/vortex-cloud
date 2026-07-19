using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class HeightMapMessageComposerSerializer(int header)
    : AbstractSerializer<HeightMapMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HeightMapMessageComposer message)
    {
        packet.WriteInteger(message.Width).WriteInteger(message.Size);

        foreach (short height in message.Heights)
        {
            packet.WriteShort(height);
        }
    }
}
