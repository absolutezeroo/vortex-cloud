using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class CollectibleMintTokenCountMessageComposerSerializer(int header)
    : AbstractSerializer<CollectibleMintTokenCountMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CollectibleMintTokenCountMessageComposer message
    )
    {
        //
    }
}
