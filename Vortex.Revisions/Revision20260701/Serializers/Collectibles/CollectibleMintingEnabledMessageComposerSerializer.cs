using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class CollectibleMintingEnabledMessageComposerSerializer(int header)
    : AbstractSerializer<CollectibleMintingEnabledMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CollectibleMintingEnabledMessageComposer message
    )
    {
        //
    }
}
