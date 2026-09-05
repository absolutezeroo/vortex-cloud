using Vortex.Primitives.Networking.Revisions;
using Vortex.Protocol.Messages.Outgoing.RewardTracks;
using Vortex.Revisions.Revision20260701.Parsers.RewardTracks;
using Vortex.Revisions.Revision20260701.Serializers.RewardTracks;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class RewardTrackMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.ClaimRewardTrackPrizeMessageEvent,
            new ClaimRewardTrackPrizeMessageParser()
        );
        builder.MapParser(
            MessageEvent.PurchaseRewardTrackPremiumMessageEvent,
            new PurchaseRewardTrackPremiumMessageParser()
        );

        builder.MapSerializer(
            typeof(RewardTracksMessageComposer),
            new RewardTracksMessageComposerSerializer(MessageComposer.RewardTracksMessageComposer)
        );
        builder.MapSerializer(
            typeof(RewardTrackProgressMessageComposer),
            new RewardTrackProgressMessageComposerSerializer(
                MessageComposer.RewardTrackProgressMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(RewardTrackClaimResultMessageComposer),
            new RewardTrackClaimResultMessageComposerSerializer(
                MessageComposer.RewardTrackClaimResultMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(RewardTrackPremiumPurchaseResultMessageComposer),
            new RewardTrackPremiumPurchaseResultMessageComposerSerializer(
                MessageComposer.RewardTrackPremiumPurchaseResultMessageComposer
            )
        );
    }
}
