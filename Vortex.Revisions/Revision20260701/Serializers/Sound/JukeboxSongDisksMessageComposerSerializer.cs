using Vortex.Primitives.Packets;
using Vortex.Primitives.Sound.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class JukeboxSongDisksMessageComposerSerializer(int header)
    : AbstractSerializer<JukeboxSongDisksMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, JukeboxSongDisksMessageComposer message)
    {
        // _SafeCls_4232.parse(): capacity, count, then (diskId, songId) pairs. Capacity comes first
        // and is easy to mistake for the count -- swapping them draws an empty jukebox.
        packet.WriteInteger(message.Capacity);
        packet.WriteInteger(message.Disks.Length);

        foreach (SongDiskSnapshot disk in message.Disks)
        {
            packet.WriteInteger(disk.DiskId);
            packet.WriteInteger(disk.SongId);
        }
    }
}
