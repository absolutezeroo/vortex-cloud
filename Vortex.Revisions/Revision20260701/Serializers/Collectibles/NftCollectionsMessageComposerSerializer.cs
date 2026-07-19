using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class NftCollectionsMessageComposerSerializer(int header)
    : AbstractSerializer<NftCollectionsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, NftCollectionsMessageComposer message)
    {
        //
    }
}
