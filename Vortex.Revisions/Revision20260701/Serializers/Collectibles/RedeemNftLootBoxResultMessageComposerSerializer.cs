using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class RedeemNftLootBoxResultMessageComposerSerializer(int header)
    : AbstractSerializer<RedeemNftLootBoxResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RedeemNftLootBoxResultMessageComposer message
    ) => packet.WriteShort(message.Status);
}
