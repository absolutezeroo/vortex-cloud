using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Collectibles.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class RedeemNftLootBoxStateMessageComposerSerializer(int header)
    : AbstractSerializer<RedeemNftLootBoxStateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RedeemNftLootBoxStateMessageComposer message
    )
    {
        packet.WriteShort(message.State).WriteInteger(message.OpenerAvatarId);

        CollectibleSerialization.WriteProductItem(packet, message.Reward);
    }
}
