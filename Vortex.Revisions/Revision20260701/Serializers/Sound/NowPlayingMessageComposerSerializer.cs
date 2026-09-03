using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class NowPlayingMessageComposerSerializer(int header)
    : AbstractSerializer<NowPlayingMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, NowPlayingMessageComposer message)
    {
        // _SafeCls_4056.parse(): currentSongId, currentPosition, nextSongId, nextPosition,
        // syncCount. The sync count is last and is the only one carrying time.
        packet.WriteInteger(message.CurrentSongId);
        packet.WriteInteger(message.CurrentIndex);
        packet.WriteInteger(message.NextSongId);
        packet.WriteInteger(message.NextIndex);
        packet.WriteInteger(message.SyncCountMs);
    }
}
