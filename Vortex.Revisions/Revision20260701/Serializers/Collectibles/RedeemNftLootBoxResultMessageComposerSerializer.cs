using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class RedeemNftLootBoxResultMessageComposerSerializer(int header)
    : AbstractSerializer<RedeemNftLootBoxResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RedeemNftLootBoxResultMessageComposer message
    ) => packet.WriteShort(message.Status);
}
