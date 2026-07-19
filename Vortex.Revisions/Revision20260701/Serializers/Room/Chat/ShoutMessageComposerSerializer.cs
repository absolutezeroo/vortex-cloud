using Vortex.Primitives.Messages.Outgoing.Room.Chat;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Chat;

internal class ShoutMessageComposerSerializer(int header)
    : AbstractSerializer<ShoutMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ShoutMessageComposer message)
    {
        packet
            .WriteInteger(message.ObjectId)
            .WriteString(message.Text)
            .WriteInteger((int)message.Gesture)
            .WriteInteger(message.StyleId)
            .WriteInteger(message.Links.Count);

        foreach ((string one, string two, bool three) in message.Links)
        {
            packet.WriteString(one).WriteString(two).WriteBoolean(three);
        }

        packet.WriteInteger(message.TrackingId);
    }
}
