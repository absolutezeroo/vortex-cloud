using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.RewardTracks;

namespace Vortex.Revisions.Revision20260701.Serializers.RewardTracks;

/// <summary>The client's <c>_SafeCls_2641.parse</c>: trackId, rewardId, resultCode.</summary>
internal class RewardTrackClaimResultMessageComposerSerializer(int header)
    : AbstractSerializer<RewardTrackClaimResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RewardTrackClaimResultMessageComposer message
    ) =>
        packet
            .WriteString(message.TrackId)
            .WriteString(message.PrizeId)
            .WriteInteger((int)message.Result);
}
