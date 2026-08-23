using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class CollectibleMintableItemResultMessageComposerSerializer(int header)
    : AbstractSerializer<CollectibleMintableItemResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CollectibleMintableItemResultMessageComposer message
    ) => packet.WriteShort(message.Status);
}
