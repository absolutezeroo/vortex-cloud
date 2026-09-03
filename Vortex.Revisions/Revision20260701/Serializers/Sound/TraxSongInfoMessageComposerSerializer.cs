using Vortex.Primitives.Packets;
using Vortex.Primitives.Sound.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class TraxSongInfoMessageComposerSerializer(int header)
    : AbstractSerializer<TraxSongInfoMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, TraxSongInfoMessageComposer message)
    {
        packet.WriteInteger(message.Songs.Length);

        foreach (SongSnapshot song in message.Songs)
        {
            packet.WriteInteger(song.Id);
            // The client reads this string and drops it on the floor — but it is the song's official
            // code, and the reference emulator writes it here too, so it stays where both agree it
            // belongs rather than being dropped from the layout.
            packet.WriteString(song.OfficialSongId);
            packet.WriteString(song.Name);
            packet.WriteString(song.Data);
            packet.WriteInteger(song.LengthMs);
            packet.WriteString(song.Creator);
        }
    }
}
