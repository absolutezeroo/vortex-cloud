using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftRewardItemClaimResultMessageComposerSerializer(int header)
    : AbstractSerializer<NftRewardItemClaimResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NftRewardItemClaimResultMessageComposer message
    ) =>
        packet
            .WriteString(message.CollectionId)
            .WriteString(message.WalletAddress)
            .WriteBoolean(message.Success);
}
