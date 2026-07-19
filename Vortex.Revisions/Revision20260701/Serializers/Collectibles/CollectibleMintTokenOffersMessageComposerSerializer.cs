using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class CollectibleMintTokenOffersMessageComposerSerializer(int header)
    : AbstractSerializer<CollectibleMintTokenOffersMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CollectibleMintTokenOffersMessageComposer message
    )
    {
        //
    }
}
