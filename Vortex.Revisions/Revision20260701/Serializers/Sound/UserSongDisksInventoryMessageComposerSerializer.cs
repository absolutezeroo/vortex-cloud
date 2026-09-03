using Vortex.Primitives.Packets;
using Vortex.Primitives.Sound.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class UserSongDisksInventoryMessageComposerSerializer(int header)
    : AbstractSerializer<UserSongDisksInventoryMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        UserSongDisksInventoryMessageComposer message
    )
    {
        packet.WriteInteger(message.Disks.Length);

        foreach (SongDiskSnapshot disk in message.Disks)
        {
            // Disk first, song second: the client builds a map keyed by the disk.
            packet.WriteInteger(disk.DiskId);
            packet.WriteInteger(disk.SongId);
        }
    }
}
