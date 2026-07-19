using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class CollectibleWalletAddressesMessageComposerSerializer(int header)
    : AbstractSerializer<CollectibleWalletAddressesMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CollectibleWalletAddressesMessageComposer message
    )
    {
        //
    }
}
