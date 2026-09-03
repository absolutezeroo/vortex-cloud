using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class OfficialSongIdMessageComposerSerializer(int header)
    : AbstractSerializer<OfficialSongIdMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, OfficialSongIdMessageComposer message)
    {
        packet.WriteString(message.OfficialSongId);
        packet.WriteInteger(message.SongId);
    }
}
