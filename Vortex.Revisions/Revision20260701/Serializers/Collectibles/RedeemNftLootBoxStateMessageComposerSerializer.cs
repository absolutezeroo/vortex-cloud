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

        // The reward is read as the BASE product struct — no amount, unlike the collections list.
        CollectibleSerialization.WriteBaseProductItem(packet, message.Reward);
    }
}
