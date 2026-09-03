using Vortex.Primitives.Packets;
using Vortex.Primitives.Sound.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class PlayListMessageComposerSerializer(int header)
    : AbstractSerializer<PlayListMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PlayListMessageComposer message)
    {
        // _SafeCls_4224.parse(): syncCount, count, then per entry id, length, name, creator. Four
        // fields, not the six of TraxSongInfo -- this one carries no composition data.
        packet.WriteInteger(message.SynchronizationCountMs);
        packet.WriteInteger(message.Songs.Length);

        foreach (SongSnapshot song in message.Songs)
        {
            packet.WriteInteger(song.Id);
            packet.WriteInteger(song.LengthMs);
            packet.WriteString(song.Name);
            packet.WriteString(song.Creator);
        }
    }
}
