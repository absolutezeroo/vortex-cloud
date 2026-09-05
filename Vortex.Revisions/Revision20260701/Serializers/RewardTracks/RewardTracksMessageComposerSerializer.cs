using Vortex.Primitives.Packets;
using Vortex.Primitives.RewardTracks.Snapshots;
using Vortex.Protocol.Messages.Outgoing.RewardTracks;

namespace Vortex.Revisions.Revision20260701.Serializers.RewardTracks;

/// <summary>
/// The client's <c>_SafeCls_2622.parse</c>: disabled, a count and that many track blocks, then
/// reload. The reload flag is LAST, after the tracks -- not beside the flag it reads like a pair
/// with.
/// </summary>
internal class RewardTracksMessageComposerSerializer(int header)
    : AbstractSerializer<RewardTracksMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RewardTracksMessageComposer message)
    {
        packet.WriteBoolean(message.Disabled).WriteInteger(message.Tracks.Length);

        foreach (RewardTrackViewSnapshot track in message.Tracks)
        {
            RewardTrackWriter.WriteTrack(packet, track);
        }

        packet.WriteBoolean(message.Reload);
    }
}
