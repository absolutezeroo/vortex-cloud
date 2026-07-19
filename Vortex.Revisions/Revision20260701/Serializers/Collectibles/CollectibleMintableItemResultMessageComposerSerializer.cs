using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class CollectibleMintableItemResultMessageComposerSerializer(int header)
    : AbstractSerializer<CollectibleMintableItemResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CollectibleMintableItemResultMessageComposer message
    )
    {
        //
    }
}
