using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.RewardTracks;

namespace Vortex.Revisions.Revision20260701.Serializers.RewardTracks;

/// <summary>
/// The client's <c>_SafeCls_3538.parse</c>: trackId, resultCode, points. Note the order differs from
/// the claim result, which puts its second id before its code.
/// </summary>
internal class RewardTrackPremiumPurchaseResultMessageComposerSerializer(int header)
    : AbstractSerializer<RewardTrackPremiumPurchaseResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RewardTrackPremiumPurchaseResultMessageComposer message
    ) =>
        packet
            .WriteString(message.TrackId)
            .WriteInteger((int)message.Result)
            .WriteInteger(message.Points);
}
