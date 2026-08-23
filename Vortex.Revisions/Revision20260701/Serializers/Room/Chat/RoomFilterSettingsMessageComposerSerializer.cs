using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Chat;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Chat;

internal class RoomFilterSettingsMessageComposerSerializer(int header)
    : AbstractSerializer<RoomFilterSettingsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomFilterSettingsMessageComposer message
    )
    {
        packet.WriteInteger(message.BadWords.Length);

        foreach (string word in message.BadWords)
        {
            packet.WriteString(word);
        }
    }
}
